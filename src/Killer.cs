/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Text;
using System.Xml;

namespace KillerOfUnwantedWindows1C.src
{
    public class Killer
    {
        private static readonly List<UnwantedTitle> _unwantedTitles = new List<UnwantedTitle>();
        private static readonly int _cycleDelay = 15 * TimeUtils.Second;
        private static readonly int _cleanupPeriod = 4 * TimeUtils.Minute;

        private List<CandidateForKilling> _candidatesForKilling = new List<CandidateForKilling>();
        private readonly int _currentSessionID;

        private WinApi.LastInputInfo _lastInputInfo = new WinApi.LastInputInfo();
        private List<Process> _pList = new List<Process>();
        private Dictionary<WindowInfo, UnwantedTitle> _unwantedWindows = new Dictionary<WindowInfo, UnwantedTitle>();
        int _cleanupTime = 0;

        public Killer(string configPath)
        {
            _currentSessionID = Process.GetCurrentProcess().SessionId;
            _lastInputInfo.cbSize = (uint)Marshal.SizeOf(_lastInputInfo);

            _candidatesForKilling.Capacity = 16;
            _pList.Capacity = 16;

            LoadConfig(configPath);
        }

        private void LoadConfig(string configPath)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(configPath);

                var items = xmlDoc.DocumentElement.SelectNodes("/UnwantedTitles/Item");
                foreach (XmlNode it in items)
                {
                    var newItem = new UnwantedTitle();
                    newItem.Title = it.Attributes["Title"].Value;
                    newItem.KillAction = it.Attributes["KillAction"].Value.ToLower() == "closeprocess" ? KillAction.kaCloseProcess : KillAction.kaCloseWindow;
                    newItem.InactivatePeriodLimit = int.Parse(it.Attributes["InactivatePeriodLimitSeconds"].Value) * 1000;
                    newItem.LifePeriodLimit = int.Parse(it.Attributes["LifePeriodLimitSeconds"].Value) * 1000;
                    _unwantedTitles.Add(newItem);
                }
            }
            catch(Exception)
            {
                Console.WriteLine($"Ошибка при загрузке файла с настройками: '{configPath}'");
                throw;
            }
        }

        public void Loop()
        {
            while (true)
            {
                CleanupMemory();

                UpdatePList();

                if (!WinApi.GetLastInputInfo(ref _lastInputInfo))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (_cleanupTime > _cleanupPeriod)
                    CleanupCandidatesForKilling();

                UpdateExistingCandidatesForKilling();

                CollectUnwantedWindows();

                foreach (var uw in _unwantedWindows)
                {
                    int foundItemIndex = _candidatesForKilling.FindIndex(it =>
                    {
                        return
                            it.Id == uw.Key.Owner.Id &&
                            it.Name == uw.Key.Owner.ProcessName &&
                            it.WindowHandle == uw.Key.Handle &&
                            it.WindowTitle == uw.Key.Title;
                    });

                    if (foundItemIndex < 0)
                    {
                        _candidatesForKilling.Add(new CandidateForKilling(
                            id: uw.Key.Owner.Id,
                            name: uw.Key.Owner.ProcessName,
                            windowTitle: uw.Key.Title,
                            windowHandle: uw.Key.Handle,
                            lastUserInputTime: _lastInputInfo.dwTime,
                            killAction: uw.Value.KillAction,
                            inactivePeriodLimit: uw.Value.InactivatePeriodLimit,
                            lifePeriodLimit: uw.Value.LifePeriodLimit));
                    }
                    else if (_candidatesForKilling[foundItemIndex].IsReadyForKilling())
                    {
                        _candidatesForKilling[foundItemIndex].Kill();
                        _candidatesForKilling.RemoveAt(foundItemIndex);
                    }
                }

                System.Threading.Thread.Sleep(_cycleDelay);

                _cleanupTime += _cycleDelay;
            }
        }

        private void CleanupMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void UpdatePList()
        {
            _pList.Clear();
            _pList.AddRange(Process.GetProcessesByName("1cv8"));
            _pList.AddRange(Process.GetProcessesByName("1cv8c"));

            _pList.RemoveAll(
                it =>
                {
                    return it.SessionId != _currentSessionID;
                });
        }

        private void CleanupCandidatesForKilling()
        {
            _cleanupTime = 0;

            for (int i = _candidatesForKilling.Count - 1; i >= 0; i--)
                if (!_pList.Exists(it =>
                {
                    return
                        _candidatesForKilling[i].Id == it.Id &&
                        _candidatesForKilling[i].Name == it.ProcessName;
                }))
                {
                    _candidatesForKilling.RemoveAt(i);
                }
        }

        private void UpdateExistingCandidatesForKilling()
        {
            for (int i = 0; i < _candidatesForKilling.Count; ++i)
            {
                var it = _candidatesForKilling[i];

                if (it.LastUserInputTime == _lastInputInfo.dwTime)
                {
                    it.InactivePeriod += _cycleDelay;
                }
                else
                {
                    it.InactivePeriod = 0;
                    it.LastUserInputTime = _lastInputInfo.dwTime;
                }

                it.LifePeriod += _cycleDelay;

                _candidatesForKilling[i] = it;
            }
        }

        private void CollectUnwantedWindows()
        {
            _unwantedWindows.Clear();

            _pList.ForEach(p => {
                var uw = GetFirstUnwantedWindow(p);
                if (uw.Key.Owner != null)
                    _unwantedWindows.Add(uw.Key, uw.Value);
            });
        }

        private KeyValuePair<WindowInfo, UnwantedTitle> GetFirstUnwantedWindow(Process p)
        {
            WindowInfo wi = new WindowInfo(null, "", IntPtr.Zero);
            UnwantedTitle ut = new UnwantedTitle();

            foreach (ProcessThread thread in p.Threads)
                WinApi.EnumThreadWindows(thread.Id,
                    (windowHandle, lParam) =>
                    {
                        if (!WinApi.IsWindowVisible(windowHandle))
                            return true;

                        int length = WinApi.GetWindowTextLength(windowHandle);
                        if (length != 0)
                        {
                            StringBuilder builder = new StringBuilder(length);
                            WinApi.GetWindowText(windowHandle, builder, length + 1);

                            string windowTitle = builder.ToString().ToLower().Trim();

                            int foundIndex = _unwantedTitles.FindIndex(it => it.Title == windowTitle);

                            if (foundIndex >= 0)
                            {
                                wi = new WindowInfo(
                                    owner: p,
                                    title: windowTitle,
                                    handle: windowHandle);

                                ut = _unwantedTitles[foundIndex];

                                return false;
                            }
                        }

                        return true;
                    }, IntPtr.Zero);

            return new KeyValuePair<WindowInfo, UnwantedTitle>(wi, ut);
        }

        private struct CandidateForKilling
        {
            private readonly int _inactivePeriodLimit;
            private readonly int _lifePeriodLimit;

            public readonly int Id;
            public readonly string Name;

            public readonly KillAction _killAction;

            public readonly string WindowTitle;
            public readonly IntPtr WindowHandle;

            public uint LastUserInputTime;

            public int LifePeriod;
            public int InactivePeriod;

            public CandidateForKilling(int id, string name, string windowTitle, IntPtr windowHandle,
                uint lastUserInputTime, KillAction killAction, int inactivePeriodLimit, int lifePeriodLimit)
            {
                Id = id;
                Name = name;
                WindowTitle = windowTitle;
                WindowHandle = windowHandle;
                LastUserInputTime = lastUserInputTime;
                _killAction = killAction;
                _inactivePeriodLimit = inactivePeriodLimit;
                _lifePeriodLimit = lifePeriodLimit;

                InactivePeriod = 0;
                LifePeriod = 0;
            }

            public bool IsReadyForKilling()
            {
                return
                    _inactivePeriodLimit > 0 && InactivePeriod > _inactivePeriodLimit ||
                    _lifePeriodLimit > 0 && LifePeriod > _lifePeriodLimit;
            }

            public void Kill()
            {
                var p = Process.GetProcessById(Id);

                if (_killAction == KillAction.kaCloseWindow)
                {
                    WinApi.SendMessage(WindowHandle, WinApi.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    Console.WriteLine($"Закрыли окно у процесса '{p.ProcessName}' по заголовку '{WindowTitle}'.");
                }
                else if (_killAction == KillAction.kaCloseProcess)
                {
                    p.Kill();
                    Console.WriteLine($"Завершили процесс '{p.ProcessName}' по заголовку '{WindowTitle}'.");
                }
                else
                    throw new Exception("unknown kill action");
            }
        }

        private struct WindowInfo
        {
            public readonly Process Owner;
            public readonly string Title;
            public readonly IntPtr Handle;

            public WindowInfo(Process owner, string title, IntPtr handle)
            {
                Owner = owner;
                Title = title;
                Handle = handle;
            }
        }

        public enum KillAction
        {
            kaCloseWindow = 0,
            kaCloseProcess = 1
        }

        struct UnwantedTitle
        {
            public string Title;
            public KillAction KillAction;
            public int InactivatePeriodLimit;
            public int LifePeriodLimit;
        }
    }
}
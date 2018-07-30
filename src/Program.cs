using System;
using System.IO;
using System.Reflection;
using KillerOfUnwantedWindows1C.src;
using KillerOfUnwantedWindows1C.src.utils;
using System.Threading;

namespace KillerOfUnwantedWindows1C
{
    class Program
    {
        // vova
        // + убрать подпись из под системы контроля версий
        // + /quiet /norestart
        // + Текст лицензии.
        // + сдeлать запрет на запуск второго экземпляра в рамках тек. пользователя
        // + сделать разные версии под разные .net с разными инсталляторами
        // + возможность устанавливать поверх друг друга одинаковые версии
        // + подпись всего
        // + убедиться что msi не зависит от версии .net на компьютере
        // + более дружелюбная иконка?
        // + сделать проверку что установлена соотв. версия .net в установщике
        // + readme.txt при установке посмотреть github ответ
        // + доработать читабельность readme        
        // + гарантировать Norestart при тихой установке
        // выложить на github
        // написать статью на сайте и связать её с грязными копиями
        // отослать на проверку
        
        const string _appLinkName = "Killer of unwanted windows 1C.lnk";
        static Mutex _mutex = new Mutex(true, "{d0156974-1ed0-42a8-813f-db6979226a46}");

        static void Main(string[] commandLine)
        {
            if (_mutex.WaitOne(TimeSpan.Zero, true))
            {
                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

                var args = new Arguments(commandLine);

                if (args["r"] != null)
                {
                    WinApi.ShowWindow(WinApi.GetConsoleWindow(), WinApi.SW_HIDE);
                    Run();
                }
                else
                {
                    Console.WriteLine($@"Убийца нежелательных окон 1С, версия {Assembly.GetExecutingAssembly().GetName().Version}
Почта для связи с автором: helpme1c.box@gmail.com

Ключи для запуска программы:
    -r запустить программу на выполнение в фоновом режиме (окно программы будет скрыто, её будет видно только в диспетчере задач)

При установке программа автоматически прописывается в автозагрузку всем пользователям и запускается с ключом -r. После установки пользователю нужно перелогиниться, чтобы программа запустилась и начала работу в фоновом режиме.
");

                    PressAnyKey();
                }

                _mutex.ReleaseMutex();
            }
            else
                Console.WriteLine("Уже работает один экземпляр программы.");
        }

        static void PressAnyKey()
        {
            Console.WriteLine("Нажмите любую клавишу, чтобы закрыть окно.");
            Console.ReadKey();
        }

        static void Run()
        {
            var configPath = Path.Combine(AppUtils.AppDir(), "config.xml");

            if (!File.Exists(configPath))
                throw new FileNotFoundException($"Не найден файл с настройками программы: '{configPath}'");

            var killer = new Killer(configPath);
            killer.Loop();
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());

            Environment.Exit(1);
        }
    }
}
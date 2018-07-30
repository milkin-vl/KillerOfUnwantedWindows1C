# KillerOfUnwantedWindows1C #
Программа "Убийца нежелательных окон 1С"

## Проблема, которую решает программа

    Внимание! Все проблемы ниже относятся только к файловым базам.

    Суть: пользователь может, не работая в базе, заблокировать её так, чтобы стали невозможны тестирование, обновление, архивация и другие операции, требующие монопольного доступа. И что самое важное: такого пользователя никак не выгнать стандартными средствами платформы.

    Примеры сценариев такой блокировки базы:

    1. Пользователь, у которого непустой пароль, оставил базу открытой и ушёл домой. Ночью запустилась программа обновления баз и выгнала этого пользователя на время операций с базой. Выгнала через стандартный механизм платформы, когда в базе создаётся объект "Блокировка сеансов". Операция с базой закончилась, блокировка сеансов была удалена и платформа вновь пустила пользователя обратно. Так как у него непустой пароль, то в базу он автоматически не попал, но осталось висеть окно авторизации. И теперь, пока пользователь не придёт и не закроет это окно, никто не сможет заблокировать базу для монопольного доступа.

    2. Пользователь попытался войти в базу, забыл пароль и оставил висеть окно авторизации. Пока будет висеть это окно, базу монопольно не заблокировать.

    3. Пользователь запустил загрузку базы и ушел, не дождавшись её окончания. Но конфигурация не загрузилась, так как появилось окно о том, что конфигурация отличается от конфигурации базы данных. Пока будет висеть эта загрузка, базу монопольно не заблокировать.

    И всё потому, что файл файловой базы данных блокируется и удерживается сразу после запуска базы, даже если пользователь ещё не прошёл авторизацию. А так как пользователь ещё не в базе, выгнать его стандартными способами нельзя.
            
    Разработанная программа решает описанные выше проблемы. Она отслеживает зависшие окна при входе в базу и своевременно их закрывает (или даже прекращает работу зависшего процесса 1С). Программу надо устанавливать на все компьютеры, с которых возможно подключение к файловым базам данных.

## Поддержка

    Автор программы: Владимир Милькин (helpme1c.ru)

    Все дистрибутивы и исполняемые файлы программы подписываются подписью автора Milkin Vladimir Vladimirovich.

    По всем вопросам, связанным с работой программы, пишите на почту автору: helpme1c.box@gmail.com

## Лицензия

    Программа опубликована на github по лицензии Mozilla Public License, version 2.0.
    С полным текстом лицензии вы можете ознакомиться в файле LICENSE в папке установки программы.
    Адрес проекта: https://github.com

## Требования к окружению

    В системе должна быть установлена одна из версий Microsoft .NET Framework.

    Среди дистрибутивов вы можете найти установочный файл для нужной вам версии .NET:    
    - setup_for_net_2_0.msi - для версии .NET 2.0 и .NET 3.5
    - setup_for_net_4_0.msi - для версии .NET 4.0 и выше

    Узнать, какая версия .NET идёт вместе с вашей ОС по умолчанию, а какая может быть установлена дополнительно, можно по этой ссылке: https://docs.microsoft.com/ru-ru/dotnet/framework/get-started/system-requirements

## Принцип работы

    1. При установке программа прописывается в автозагрузку всех пользователей компьютера.

    2. В момент авторизации пользователя в его сессии в скрытом режиме запускается KillerOfUnwantedWindows1C.exe.

    3. Процесс всё время висит в фоне, практически не потребляя ресурсов.

    4. Каждые 15 секунд этот процесс проверяет:
        - есть ли запущенные экземпляры 1С (1cv8.exe и 1cv8c.exe)
        - если есть, то имеются ли у них видимые окна, заголовки которых совпадают с одними из тех, что прописаны в config.xml
        - если такие заголовки находятся, то они запоминаются

    5. Как идёт учёт нежелательных заголовков:
        - у каждого заголовка, описанного в config.xml, есть следующие поля:

            Title: текст заголовка, точное совпадение с которым, означает, что данное окно является нежелательным.
            
            InactivatePeriodLimitSeconds: интервал в секундах. Если окно с этим заголовком висит в течение указанного интервала (реальное значение может отличаться в большую сторону на 15 секунд), и при этом пользователь не шевелит мышкой, не нажимает на кнопки мыши и не вводит ничего на клавиатуре, это означает что данное окно должно быть убито (о методах убийства см. ниже). При значении 0 параметр не учитывается, и окно может висеть сколь угодно долго, но тогда должен быть не нулевым параметр LifePeriodLimitSeconds.

            LifePeriodLimitSeconds: интервал в секундах. Если окно с этим заголовком висит в течение указанного интервала (реальное значение может отличаться в большую сторону на 15 секунд), и при этом не важно работает пользователь за компьютером или нет, это означает, что данное окно должно быть убито (о методах убийства см. ниже). При значении 0 параметр не учитывается, и окно может висеть сколь угодно долго, но тогда должен быть не нулевым параметр InactivatePeriodLimitSeconds.
            
            KillAction: метод убийства окна. Возможны значения: "CloseWindow" (окну посылается сигнал для его закрытия) или "CloseProcess" (принудительно закрывается весь процесс 1С, с которым связано данное окно)

== Настройки по умолчанию и их смысл ==

    По умолчанию config.xml выглядит следующим образом:

    <UnwantedTitles>
        <Item Title="1с:предприятие. доступ к информационной базе" KillAction="CloseWindow" InactivatePeriodLimitSeconds="30" LifePeriodLimitSeconds="120"/>
        <Item Title="доступ к информационной базе" KillAction="CloseProcess" InactivatePeriodLimitSeconds="0" LifePeriodLimitSeconds="600"/>
        <Item Title="загрузка конфигурационной информации..." KillAction="CloseProcess" InactivatePeriodLimitSeconds="0" LifePeriodLimitSeconds="600"/>
    </UnwantedTitles>

    Что означает:
        Закрывать окно 1С с заголовком "1с:предприятие. доступ к информационной базе", которое висит более 30 (+15) секунд при неактивном пользователе.
        Закрывать окно 1С с заголовком "1с:предприятие. доступ к информационной базе" висит более 120 (+15) секунд в любом случае.
        Закрывать процесс 1С, у которого висит окно "доступ к информационной базе" в течение 10 минут.
        Закрывать процесс 1С, у которого висит окно "загрузка конфигурационной информации..." в течение 10 минут.

    Эти настройки и временные интервалы подобранны таким образом, чтобы:
        - закрывать открытые окна авторизации в 1С, с которыми не взаимодействует пользователь
        - закрывать зависшую загрузку базы (более 10 минут)
        - но при всё при этом не создавать помех пользователю, который просто долго выбирает под кем войти в базу и вводит пароль, возможно не с первой попытки и т.д.

    В файл всё это вынесено не столько для изменения временных интервалов или добавления новых заголовков - здесь я советую ничего не трогать. Сколько для локализации решения, так как на платформах 1С с другими языками эти фразы будут другими.

## Локализация

    Измените файл config.xml на соотв. сообщения платформы на вашем языке. Можете также прислать его мне - я включу его в цепочку подготовки дистрибутивов и буду выпускать программу и для вашего языка.
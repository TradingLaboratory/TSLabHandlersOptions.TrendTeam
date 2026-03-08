# QWEN.md — Контекст проекта TSLabHandlersOptions.TrendTeam

## 📋 Обзор проекта

**TSLabHandlersOptions.TrendTeam** — это библиотека обработчиков (handlers) для платформы TSLab, предназначенная для торговли опционами. Проект предоставляет широкий набор инструментов для работы с опционными стратегиями, включая расчёт греков, управление позициями, работу с волатильностью и автоматический хеджирование.

### Ключевые возможности

- **Расчёт греков опционов**: Delta, Gamma, Theta, Vega, Vomma по модели Блэка-Шолза
- **Управление позициями**: Открытие/закрытие виртуальных позиций, учёт комиссий
- **Волатильность**: Расчёт подразумеваемой волатильности (IV), построение улыбок волатильности
- **Автоматическое хеджирование**: Автоматическая балансировка дельты портфеля
- **Визуализация**: Отображение опционной доски, профилей позиций, улыбок волатильности

---

## 🏗️ Архитектура проекта

### Структура каталогов

```
TSLabHandlersOptions.TrendTeam/
├── Options/                    # Основные классы обработчиков (150+ файлов)
│   ├── OptionsBoardHandler.cs  # Главный обработчик опционной доски
│   ├── PositionsManager.cs     # Управление позициями
│   ├── SmileImitation5.cs      # Моделирование улыбки волатильности
│   ├── Numerical*.cs           # Расчёт численных греков
│   ├── AutoHedger.cs           # Автоматическое хеджирование
│   ├── QuoteIv.cs              # Котирование по подразумеваемой волатильности
│   └── ...                     # Другие обработчики
│
├── OptionsPublic/              # Публичные классы с упрощённым интерфейсом
│   ├── BasePxPublic.cs
│   ├── BuyOptionsPublic.cs
│   ├── GlobalHvPublic.cs
│   ├── HVPublic.cs
│   ├── SmileImitation3Public.cs
│   ├── SmileFunction3Public.cs
│   └── TimeToExpiryPublic.cs
│
├── FinMath.cs                  # Финансовая математика (Блэк-Шолз)
├── StatMath.cs                 # Статистическая математика (нормальное распределение)
├── Logging.cs                  # Система логирования
├── ScriptHandlers.Options.csproj
└── TSLabHandlers.Options.TrendTeam.sln
```

### Основные компоненты

| Компонент | Назначение |
|-----------|------------|
| `OptionsBoardHandler` | Центральный обработчик для работы с опционной доской |
| `PositionsManager` | Управление опционными и фьючерсными позициями |
| `FinMath` | Функции для расчёта цен опционов и греков |
| `StatMath` | Функции нормального распределения для финансовых расчётов |
| `SmileImitation5` | Моделирование улыбки подразумеваемой волатильности |
| `AutoHedger` | Автоматическое хеджирование дельты портфеля |
| `NumericalDelta/Gamma/Theta/Vega` | Расчёт численных греков опционов |

---

## 🛠️ Технологии и зависимости

### Основные технологии

- **Язык**: C# (.NET 9.0)
- **IDE**: Visual Studio 2022 / Visual Studio 2026
- **Платформа**: TSLab 2.2+
- **Nullable reference types**: Отключены (`<Nullable>disable</Nullable>`)

### NuGet пакеты

```xml
<TSLab.Script.Handlers Version="3.0.0" />  <!-- TSLab API -->
```

### Внешние зависимости

- **TSLab DLL**: Требуется установка TSLab 2.2
  - `TSLab.Script`
  - `TSLab.Script.Handlers`
  - `TSLab.Script.CanvasPane`
  - `TSLab.Script.Options`

---

## 🚀 Сборка и запуск

### Требования

- **ОС**: Windows 10/11 или Windows Server
- **.NET**: .NET 9.0 SDK
- **TSLab**: версия 2.2+ (установлен в `C:\Program Files\TSLab\`)
- **Visual Studio**: 2022 или 2026

### Сборка проекта

```powershell
# Перейти в директорию проекта
cd C:\Users\vdv-v\source\repos\TSLabHandlersOptions.TrendTeam

# Сборка через dotnet CLI
dotnet build TSLabHandlers.Options.TrendTeam.sln --configuration Release

# Отладочная сборка
dotnet build ScriptHandlers.Options.csproj --configuration Debug
```

### Развёртывание в TSLab

После сборки DLL файл располагается в:
```
bin/Release/net9.0/TSLab.Script.Handlers.Options.dll
```

Скопируйте DLL в папку плагинов TSLab:
```
C:\Program Files\TSLab\TSLab 2.2\Plugins\
```

---

## 📦 Основные классы и обработчики

### Обработчики для работы с позициями

| Класс | Описание |
|-------|----------|
| `PositionsManager` | Управление опционными позициями, учёт PnL |
| `OpenVirtualOptPosition` | Открытие виртуальной опционной позиции |
| `CloseVirtualPosition` | Закрытие виртуальной позиции |
| `SingleSeriesPositionGrid` | Сетка позиций для визуализации |
| `SingleSeriesPositionPrices` | Цены позиций |
| `TotalProfit` | Общая прибыль/убыток портфеля |
| `TotalQty` | Общее количество контрактов |

### Обработчики для расчёта греков

| Класс | Описание |
|-------|----------|
| `NumericalDeltaOnF3` | Расчёт дельты опциона |
| `NumericalGammaOnF3` | Расчёт гаммы опциона |
| `NumericalThetaOnF` | Расчёт теты опциона |
| `NumericalVegaOnF` | Расчёт веги опциона |
| `NumericalVommaOnF` | Расчёт воммы опциона |
| `SingleSeriesNumericalDelta3` | Дельта для серии опционов |
| `BlackScholesGreeks` | Греки по модели Блэка-Шолза |

### Обработчики для работы с волатильностью

| Класс | Описание |
|-------|----------|
| `SmileImitation5` | Моделирование улыбки волатильности |
| `IvSmile2` | Подразумеваемая волатильность по страйкам |
| `GlobalHv` | Историческая волатильность |
| `QuoteIv` | Котирование по заданной волатильности |
| `TransformSmile` | Трансформация улыбки волатильности |
| `EditTemplateSmile` | Редактирование шаблона улыбки |

### Обработчики для торговли

| Класс | Описание |
|-------|----------|
| `BuyOptions` | Покупка опционов |
| `SellOptions` | Продажа опционов |
| `BuyOptionGroup` | Покупка группы опционов |
| `SellOptionGroup` | Продажа группы опционов |
| `AutoHedger` | Автоматическое хеджирование |
| `ChartTrading` | Торговля с графика |
| `BestChartTrading` | Лучшая цена для торговли |

### Вспомогательные обработчики

| Класс | Описание |
|-------|----------|
| `BasePx2` | Базовая цена актива |
| `CurrentFutPx` | Текущая цена фьючерса |
| `TimeToExpiry` | Время до экспирации |
| `OptionSeriesByNumber2` | Выбор серии опционов |
| `SetViewport` | Управление областью отображения |
| `VerticalLine2` | Вертикальная линия на графике |

---

## 🧮 Финансовая математика

### Класс `FinMath`

Основные методы для расчётов по модели Блэка-Шолза:

```csharp
// Расчёт цены опциона
double GetOptionPrice(double basePrice, double strike, double expTime, 
                      double sigma, double pctRate, bool isCall = true)

// Расчёт цены стреддла
double GetStradlePrice(double basePrice, double strike, double expTime, 
                       double sigma, double pctRate)

// Расчёт подразумеваемой волатильности по цене опциона
double GetOptionSigma(double basePrice, double strike, double expTime, 
                      double optPrice, double pctRate, bool isCall)

// Расчёт подразумеваемой волатильности по цене стреддла
double GetStradleSigma(double basePrice, double strike, double expTime, 
                       double optPrice, double pctRate)

// Расчёт дельты опциона
double GetOptionDelta(double basePrice, double strike, double expTime, 
                      double sigma, double pctRate, bool isCall = true)

// Расчёт теты опциона
double GetOptionTheta(double basePrice, double strike, double expTime, 
                      double sigma, double pctRate, bool isCall = true)

// Расчёт веги опциона
double GetOptionVega(double basePrice, double strike, double expTime, 
                     double sigma, double pctRate, bool isCall = true)

// Расчёт гаммы опциона
double GetOptionGamma(double basePrice, double strike, double expTime, 
                      double sigma, double pctRate, bool isCall = true)

// Пересчёт IV к другому времени
double RescaleIvToAnotherTime(double oldT, double oldSigma, double newT)
```

### Класс `StatMath`

Статистические функции для финансовых расчётов:

```csharp
// Интеграл вероятности (Error Function)
double Erf(double value)

// Обратный интеграл вероятности
double InvErf(double value)

// Дополнительный интеграл вероятности
double ErfC(double value)

// Функция нормального распределения
double NormalDistribution(double value)

// Обратная функция нормального распределения
double InvNormalDistribution(double value)
```

---

## 📝 Практики разработки

### Соглашения по коду

- **Nullable reference types**: Отключены (`<Nullable>disable</Nullable>`)
- **Target Framework**: .NET 9.0
- **Именование**: PascalCase для классов и методов
- **Комментарии**: Двухязычные (русский/английский) через XML-документацию

### Структура обработчиков TSLab

Каждый обработчик наследуется от базовых классов TSLab:

```csharp
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.Script.Handlers.Options;

public class MyCustomHandler : IExternalOptionScript
{
    // Реализация обработчика
}
```

### Оптимизационные свойства

Для параметров, доступных для оптимизации, используются специальные классы:

```csharp
public OptimProperty SomeParameter = new OptimProperty(
    defaultValue: 30.0,
    isVisible: false,
    minValue: 0.000001,
    maxValue: 10000.0,
    step: 0.5,
    decimals: 1
);

public BoolOptimProperty BoolParameter = new BoolOptimProperty(
    defaultValue: true,
    isVisible: false
);

public EnumOptimProperty EnumParameter = new EnumOptimProperty(
    StrikeType.Any,
    false
);
```

---

## 🔧 Конфигурация

### Параметры опционной доски

Основные настраиваемые параметры в `OptionsBoardHandler`:

| Параметр | Описание | Значение по умолчанию |
|----------|----------|----------------------|
| `Expiry` | Дата экспирации (dd-MM-yyyy HH:mm) | "15-12-2014 18:45" |
| `TimeMode` | Режим расчёта времени | `RtsTradingTime` |
| `BasePriceMode` | Режим базовой цены | `LastTrade` |
| `SmileMode` | Тип улыбки волатильности | `Market` |
| `RiskFreeRatePct` | Безрисковая ставка (%) | 0 |
| `StrikesStep` | Шаг страйков | 0 |

### Параметры улыбки волатильности

```csharp
// Параметры модельной улыбки (HedgeSmile)
public OptimProperty HedgeSmileIvAtmPct = new OptimProperty(30.0, ...);
public OptimProperty HedgeSmileSlopePct = new OptimProperty(0.0, ...);
public OptimProperty HedgeSmileShapePct = new OptimProperty(0.0, ...);

// Параметры портфельной улыбки (PortfolioSmile)
public OptimProperty PortfolioSmileIvAtmPct = new OptimProperty(30.0, ...);
public OptimProperty PortfolioSmileSlopePct = new OptimProperty(-10.0, ...);
public OptimProperty PortfolioSmileShapePct = new OptimProperty(0.0, ...);
```

### Параметры автоматического хеджирования

```csharp
public OptimProperty AutoHedgeTargetDelta = new OptimProperty(0.0, ...);
public OptimProperty AutoHedgeUpDelta = new OptimProperty(1.0, ...);
public OptimProperty AutoHedgeDownDelta = new OptimProperty(-1.0, ...);
public OptimProperty AutoHedgeSensitivityPct = new OptimProperty(66, ...);
public OptimProperty AutoHedgeMinPeriod = new OptimProperty(0, ...);
```

---

## 🐛 Отладка и решение проблем

### Частые проблемы

1. **Отсутствуют ссылки на TSLab DLL**
   - Проверить установку TSLab 2.2 в `C:\Program Files\TSLab\`
   - Убедиться, что пакет `TSLab.Script.Handlers` установлен через NuGet

2. **Ошибки сборки после обновления .NET**
   - Очистить решение: `dotnet clean`
   - Восстановить пакеты: `dotnet restore`
   - Пересобрать: `dotnet build`

3. **Обработчик не отображается в TSLab**
   - Проверить, что DLL скопирован в папку плагинов TSLab
   - Перезапустить TSLab
   - Проверить версию .NET (должна совпадать с версией TSLab)

### Логирование

Используется система трассировки .NET через класс `Logging`:

```csharp
// Включение логирования
Logging.On  // true/false

// Запись сообщений
Logging.Handlers.TraceEvent(TraceEventType.Information, 0, "Message");
Logging.PrintInfo(Logging.Handlers, "ObjectName", "Info message");
Logging.PrintWarning(Logging.Handlers, "Warning message");
Logging.PrintError(Logging.Handlers, "Error message");
```

Настройка уровня логирования в `app.config` TSLab:
```xml
<system.diagnostics>
  <sources>
    <source name="TSLab.Script.Handlers.Options" switchValue="Information">
      <listeners>
        <add name="console" />
      </listeners>
    </source>
  </sources>
</system.diagnostics>
```

---

## 📖 Типичные сценарии использования

### 1. Создание нового обработчика

```csharp
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.Script.Handlers.Options;

public class MyCustomHandler : IExternalOptionScript
{
    // Объявление параметров
    public OptimProperty MyParameter = new OptimProperty(100, true, 0, 1000, 1, 0);
    
    // Объявление зависимых обработчиков
    private BasePx2 m_basePriceH = new BasePx2();
    
    public void OnInitialize(IContext context)
    {
        // Инициализация
    }
    
    public void OnExecute()
    {
        // Логика выполнения
    }
    
    public void Dispose()
    {
        // Очистка ресурсов
    }
}
```

### 2. Расчёт цены опциона

```csharp
using TSLab.Script.Handlers;

var finMath = new FinMath();

// Параметры
double basePrice = 120000;      // Цена базового актива
double strike = 125000;         // Страйк
double expTime = 30.0 / 365;    // Время до экспирации (в годах)
double sigma = 0.35;            // Волатильность (35%)
double pctRate = 7.5;           // Безрисковая ставка (%)
bool isCall = true;             // CALL опцион

// Расчёт цены
double optionPrice = FinMath.GetOptionPrice(
    basePrice, strike, expTime, sigma, pctRate, isCall);

// Расчёт греков
double delta = FinMath.GetOptionDelta(
    basePrice, strike, expTime, sigma, pctRate, isCall);
double gamma = FinMath.GetOptionGamma(
    basePrice, strike, expTime, sigma, pctRate, isCall);
double theta = FinMath.GetOptionTheta(
    basePrice, strike, expTime, sigma, pctRate, isCall);
double vega = FinMath.GetOptionVega(
    basePrice, strike, expTime, sigma, pctRate, isCall);
```

### 3. Управление позициями

```csharp
// Открытие виртуальной позиции
var openPosition = new OpenVirtualOptPosition2
{
    Context = context,
    OptionType = StrikeType.Call,
    Strike = 125000,
    Qty = 10,
    Price = 2500
};

// Закрытие позиции
var closePosition = new CloseVirtualPosition
{
    Context = context,
    PositionId = positionId
};

// Получение информации о позиции
var posManager = new PositionsManager();
var positionInfo = posManager.GetPositionInfo(symbol);
```

---

## 📊 Статистика проекта (на март 2026)

- **Всего файлов .cs**: 159
- **Обработчиков в папке Options**: 150+
- **Публичных классов в OptionsPublic**: 7
- **Target Framework**: .NET 9.0
- **Версия сборки**: 3.0.0.0
- **Основной язык**: C#

---

## 🔗 Связанные проекты

Этот проект является частью экосистемы TrendTeamTrading:

- **TrendTeamTrading** (`C:\Users\vdv-v\source\repos\TrendTeamTrading`) — основные стратегии и хендлеры
- **TaLib.TrendTeam** (`C:\Users\vdv-v\source\repos\TaLib.TrendTeam`) — библиотека технического анализа
- **TrendTeamTrader** (`C:\Users\vdv-v\source\repos\TrendTeamTrader`) — автономная торговая система
- **wiki** (`C:\Users\vdv-v\source\repos\wiki`) — база знаний

---

## 📚 Дополнительные ресурсы

### Внутренняя документация

- **README.md**: Краткое описание проекта
- **FinMath.cs**: Финансовая математика (Блэк-Шолз)
- **StatMath.cs**: Статистические функции

### Внешние ресурсы

- **TSLab Official**: https://tslab.ru/
- **TSLab Documentation**: https://tslab.ru/support
- **Black-Scholes Model**: https://en.wikipedia.org/wiki/Black%E2%80%93Scholes_model
- **Options Greeks**: https://www.investopedia.com/terms/g/greeks.asp

---

*Последнее обновление: 9 марта 2026 г.*

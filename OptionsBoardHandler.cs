using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

using TSLab.DataSource;
using TSLab.Script;
using TSLab.Script.CanvasPane;
using TSLab.Script.Control;
using TSLab.Script.DataGridPane;
using TSLab.Script.Handlers;
using TSLab.Script.Handlers.Options;
using TSLab.Script.Optimization;
using TSLab.Script.Options;
using TSLab.Utils;

// ReSharper disable UnusedMember.Global
// ReSharper disable TooWideLocalVariableScope
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace TSLab.ScriptEngine.Handlers
{
    // ReSharper disable once UnusedMember.Global
    public sealed class OptionsBoardHandler : IExternalOptionScript, IDisposable
    {
        /// <summary>При работе с зоной валидных значений нужно сразу ставить очень широкий диапазон, но не MaxValue!</summary>
        private const double ValidBorderMaxValue = 1e6;

        private const string DefaultPx = "0";
        // TODO: менять формат в зависимости от шага цены?
        private const string AveragePriceFormat = "### ### ##0.###";
        /// <summary>
        /// Формат экспирации должен соблюдаться крайне строго: dd-MM-yyyy HH:mm
        /// Он должен совпадать с форматом TimeToExpiry.DateTimeFormat
        /// </summary>
        private const string DefaultExpiration = "15-12-2014 18:45";

        /// <summary>Темно-красный: 12603469 == 0xC0504D</summary>
        private const int DarkRed = 0xC0504D;

        /// <summary>Цвет линий, связанных с рыночной улыбкой (12603469; Темно-красный; 0xC0504D)</summary>
        private const int MarketProfileLineColor = DarkRed; // Темно-красный: 0xC0504D
        /// <summary>Цвет якорей, связанных с рыночной улыбкой (16776960; Yellow; 0xFFFF00)</summary>
        private int MarketProfileAnchorColor = AlphaColors.Yellow; // Yellow: 0xFFFF00 == 16776960
        /// <summary>Цвет линий, связанных с модельной улыбкой (16777215; White; 0xFFFFFF or Black; 0x0F0F0F)</summary>
        private int ModelProfileLineColor = AlphaColors.White; // White: 0xFFFFFF or Black: 0x0F0F0F == 16777215
        ///// <summary>Цвет якорей, связанных с модельной улыбкой (; Yellow; 0xFFFF00)</summary>
        //private const int ModelProfileAnchorColor = ; // Yellow: 0xFFFF00
        /// <summary>Цвет линий, связанных с портфельной улыбкой (-16711936; Green; 0x00FF00)</summary>
        private int PortfolioProfileLineColor = AlphaColors.Green; // Green: 0x00FF00 == -16711936
        /// <summary>Цвет якорей, связанных с портфельной улыбкой (16776960; Yellow; 0xFFFF00 or Magenta; 0xFF00FF)</summary>
        private int PortfolioProfileAnchorColor = AlphaColors.Yellow; // Yellow: 0xFFFF00 == 16776960
        /// <summary>Цвет линий, связанных с исторической волатильностью (16760832; Orange; 0xFFC000)</summary>
        private const int HvProfileLineColor = 16760832; // Orange: 0xFFC000

        /// <summary>UiControlFirstTop == 5 (положение по вертикали вехнего угла первого контрола на панели)</summary>
        private const double UiControlFirstTop = 5;
        /// <summary>UiControlLeft == 5 (отступ по горизонтали от края панели)</summary>
        private const double UiControlLeft = 5;
        /// <summary>UiControlFullWidth == 180</summary>
        private const double UiControlFullWidth = 180;
        /// <summary>UiControlHalfWidth == 90</summary>
        private const double UiControlHalfWidth = 90;
        /// <summary>UiControlBtnHeight == 30</summary>
        private const double UiControlBtnHeight = 30;
        /// <summary>UiControlDropHeight == 45</summary>
        private const double UiControlDropHeight = 45;
        /// <summary>UiControlNumHeight == 45</summary>
        private const double UiControlNumHeight = 45;
        /// <summary>UiControlUpDownHeight == 45</summary>
        private const double UiControlUpDownHeight = 45;
        /// <summary>UiControlChkBoxHeight == 30</summary>
        private const double UiControlChkBoxHeight = 30;

        #region Fields - Handlers
        private PositionsManager m_posManH = new PositionsManager();

        private OptionSeriesByNumber2 m_nearOptionsH = new OptionSeriesByNumber2();

        private SingleSeriesProfile m_mktProfileH = new SingleSeriesProfile();

        /// <summary>Профиль позиции по портфельной улыбке PROD-5352 [2017-10-04]</summary>
        private SingleSeriesProfile m_portfolioProfileH = new SingleSeriesProfile();

        private SingleSeriesProfile m_expiryProfileH = new SingleSeriesProfile();

        private FixedValue m_zeroDtH = new FixedValue();

        private SingleSeriesPositionGrid m_qtyCallsH = new SingleSeriesPositionGrid();

        private SingleSeriesPositionGrid m_qtyPutsH = new SingleSeriesPositionGrid();

        private SingleSeriesPositionGrid m_qtyTotalH = new SingleSeriesPositionGrid();

        private AutoHedger m_autoHedgeH = new AutoHedger();

        private CurrentFutPx m_currentFutPxH = new CurrentFutPx();

        private CurrentFutPx m_currentFutPx1H = new CurrentFutPx();

        private NumericalVegaOnF m_numerVegaAtmH = new NumericalVegaOnF();

        private TotalQty m_totalQtyH = new TotalQty();

        private OptionBase2 m_optionBase2H = new OptionBase2();

        private NumericalThetaOnF m_numerThetaAtmH = new NumericalThetaOnF();

        private SingleSeriesNumericalDelta3 m_sinSerNumDelIntH = new SingleSeriesNumericalDelta3();

        private NumericalGammaOnF3 m_numGamAtmIntH = new NumericalGammaOnF3();

        private ConstSmileLevel2 m_zeroPnLh = new ConstSmileLevel2();

        private BasePx2 m_basePriceH = new BasePx2();

        private TimeToExpiry m_dTh = new TimeToExpiry();

        private IvSmile2 m_ivAsksH = new IvSmile2();

        private IvSmile2 m_ivBidsH = new IvSmile2();

        private TransformSmile m_wrapSmileH = new TransformSmile();

        private BestChartTrading m_tradeAsksH = new BestChartTrading();

        private BestChartTrading m_tradeBidsH = new BestChartTrading();

        private ExchangeTheorSigma5 m_exSmileRescaledH = new ExchangeTheorSigma5();

        private SmileImitation5 m_globalSmileH = new SmileImitation5();

        private SingleSeriesProfile m_mktSimmProfileH = new SingleSeriesProfile();

        private SingleSeriesNumericalDelta3 m_ssndiSimmH = new SingleSeriesNumericalDelta3();

        private NumericalDeltaOnF3 m_ndaiSimmH = new NumericalDeltaOnF3();

        private SingleSeriesNumericalGamma3 m_ssngiSimmH = new SingleSeriesNumericalGamma3();

        private NumericalDeltaOnF3 m_numDeltaAtmIntH = new NumericalDeltaOnF3();

        private SmileImitation5 m_hedgeSmileH = new SmileImitation5();

        /// <summary>Портфельная улыбка PROD-5352 [2017-10-04]</summary>
        private SmileImitation5 m_portfolioSmileH = new SmileImitation5();

        private TransformSmile m_simmSmileH = new TransformSmile();

        private Multiply m_wrapDtH = new Multiply();

        private LinearTransform m_wrapFutPxH = new LinearTransform();

        private SetViewport m_manageSmilePaneH = new SetViewport();

        private SetViewport m_managePosPaneH = new SetViewport();

        private SingleSeriesPositionPrices m_longCallPxsH = new SingleSeriesPositionPrices();

        private SingleSeriesPositionPrices m_longPutPxsH = new SingleSeriesPositionPrices();

        private SingleSeriesPositionPrices m_shortPutPxsH = new SingleSeriesPositionPrices();

        private SingleSeriesPositionPrices m_shortCallPxsH = new SingleSeriesPositionPrices();

        private SingleSeriesPositionPrices m_longCallQtyH = new SingleSeriesPositionPrices();

        private SingleSeriesPositionPrices m_longPutQtyH = new SingleSeriesPositionPrices();

        private SingleSeriesPositionPrices m_shortPutQtyH = new SingleSeriesPositionPrices();

        private SingleSeriesPositionPrices m_shortCallQtyH = new SingleSeriesPositionPrices();

        private QuoteIv m_quoteIvH = new QuoteIv();

        private ShowIvTargets m_showLongTargetsH = new ShowIvTargets();

        private ShowIvTargets m_showShortTargetsH = new ShowIvTargets();

        private TotalProfit m_totalProfitH = new TotalProfit();

        private GlobalHv m_globalHvH = new GlobalHv();

        private BlackScholesSmile2 m_blackScholseSmileH = new BlackScholesSmile2();

        // TODO: Убрать, когда появится возможность задавать пересчеты агента строго на границе бара по таймеру (переделать Доску на эту фичу)
        private Heartbeat m_heartbeat = new Heartbeat();
        #endregion Fields - Handlers

        #region Fields - Optim properties
        public BoolOptimProperty PosManAgregatePositions = new BoolOptimProperty(true, false);

        public BoolOptimProperty PosManBlockTrading = new BoolOptimProperty(false, false);

        public BoolOptimProperty PosManDropVirtualPos = new BoolOptimProperty(false, false);

        public BoolOptimProperty PosManUseVirtualPositions = new BoolOptimProperty(false, false);

        public StringOptimProperty DtExpiryTime = new StringOptimProperty("18:45", false);

        public OptimProperty DtTime = new OptimProperty(0.08, true, 1e-9, 100, 0.001, 3);

        public OptimProperty BasePriceDisplayPrice = new OptimProperty(120000, true, 1e-9, 100000000, 1, 1);

        public BoolOptimProperty BasePriceRepeatLastPx = new BoolOptimProperty(true, false);

        public OptimProperty ExSmileRescaledSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        public OptimProperty GlobalSmileIvAtmPct = new OptimProperty(30.0, false, 0.000001, 10000.0, 0.5, 1);

        public BoolOptimProperty GlobalSmileSetIvByHands = new BoolOptimProperty(false, false);

        public BoolOptimProperty GlobalSmileSetShapeByHands = new BoolOptimProperty(false, false);

        public BoolOptimProperty GlobalSmileSetSlopeByHands = new BoolOptimProperty(false, false);

        /// <summary>По умолчанию 0</summary>
        public OptimProperty GlobalSmileShapePct = new OptimProperty(0.0, false, -1000000.0, 1000000.0, 0.5, 1);

        /// <summary>По умолчанию 7</summary>
        public OptimProperty GlobalSmileSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        public OptimProperty GlobalSmileSlopePct = new OptimProperty(-10.0, false, -10000.0, 10000.0, 0.5, 1);

        public IntOptimProperty MktProfileNodesCount = new IntOptimProperty(0, false, 0, 1000000, 1);

        public OptimProperty MktProfileSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        public StringOptimProperty MktProfileTooltipFormat = new StringOptimProperty("0", false);

        public IntOptimProperty ExpiryProfileNodesCount = new IntOptimProperty(0, false, 0, 1000000, 1);

        public BoolOptimProperty ExpiryProfileShowNodes = new BoolOptimProperty(false, false);

        public OptimProperty ExpiryProfileSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        public StringOptimProperty ExpiryProfileTooltipFormat = new StringOptimProperty("0", false);

        public StringOptimProperty QtyCallsTooltipFormat = new StringOptimProperty("0", false);

        public StringOptimProperty QtyPutsTooltipFormat = new StringOptimProperty("0", false);

        public StringOptimProperty QtyTotalTooltipFormat = new StringOptimProperty("0", false);

        public OptimProperty HedgeSmileIvAtmPct = new OptimProperty(30.0, false, 0.000001, 10000.0, 0.5, 1);

        public BoolOptimProperty HedgeSmileSetIvByHands = new BoolOptimProperty(false, false);

        // [2016-03-31] PROD-3345 - Фиксирую Shape == 0
        /// <summary>По умолчанию 0</summary>
        public OptimProperty HedgeSmileShapePct = new OptimProperty(0.0, false, -1000000.0, 1000000.0, 0.5, 1);

        public OptimProperty HedgeSmileSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        // [2016-10-21] PROD-4340 - Другой алгоритм построения модельной улыбки
        /// <summary>По умолчанию 0</summary>
        public OptimProperty HedgeSmileSlopePct = new OptimProperty(0.0, false, -1000000.0, 1000000.0, 0.5, 1);

        // [2017-10-04] PROD-5352 - Параметры портфельной улыбки
        public BoolOptimProperty PortfolioSmileSetIvByHands = new BoolOptimProperty(false, false);
        public BoolOptimProperty PortfolioSmileSetSlopeByHands = new BoolOptimProperty(false, false);
        //public BoolOptimProperty PortfolioSmileSetShapeByHands = new BoolOptimProperty(true, false); -- Форма всегда задается руками
        public OptimProperty PortfolioSmileIvAtmPct = new OptimProperty(30.0, false, 0.000001, 10000.0, 0.5, 1);
        public OptimProperty PortfolioSmileSlopePct = new OptimProperty(-10.0, false, -1000000.0, 1000000.0, 0.5, 1);
        public OptimProperty PortfolioSmileShapePct = new OptimProperty(0.0, false, -1000000.0, 1000000.0, 0.5, 1);

        // [2016-10-21] PROD-4340 - Другой алгоритм построения модельной улыбки
        public OptimProperty SimmWeight = new OptimProperty(0.5, false, -5.0, 5.0, 0.05, 2);

        public IntOptimProperty MktSimmProfileNodesCount = new IntOptimProperty(0, false, 0, 1000000, 1);

        public OptimProperty MktSimmProfileSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        public StringOptimProperty MktSimmProfileTooltipFormat = new StringOptimProperty("0", false);

        public OptimProperty SsndiSimmSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        public OptimProperty NdaiSimmDelta = new OptimProperty(0, true, -1000000D, 1000000D, 1, 6);

        public BoolOptimProperty NdaiSimmHedgeDelta = new BoolOptimProperty(false, false);

        public BoolOptimProperty NdaiSimmPrintDeltaInLog = new BoolOptimProperty(false, false);

        public OptimProperty AutoHedgeBuyShift = new OptimProperty(0, false, -1000000, 1000000, 1, 0);

        public OptimProperty AutoHedgeSellShift = new OptimProperty(0, false, -1000000, 1000000, 1, 0);

        public OptimProperty AutoHedgeBuyPrice = new OptimProperty(120000, true, 0, 100000000, 1, 4);

        public OptimProperty AutoHedgeSellPrice = new OptimProperty(120000, true, 0, 100000000, 1, 4);

        public BoolOptimProperty AutoHedgeHedgeDelta = new BoolOptimProperty(false, false);

        public OptimProperty AutoHedgeMinPeriod = new OptimProperty(0, false, 0, 1000000, 1, 0);

        public OptimProperty AutoHedgeSensitivityPct = new OptimProperty(66, false, 50, 100, 1, 0);

        public OptimProperty AutoHedgeDownDelta = new OptimProperty(-1.0, false, -1000000.0, 0, 1.0, 1);

        public OptimProperty AutoHedgeTargetDelta = new OptimProperty(0.0, false, -1000000.0, 1000000.0, 1.0, 1);

        public OptimProperty AutoHedgeUpDelta = new OptimProperty(1.0, false, 0, 1000000.0, 1.0, 1);

        public IntOptimProperty CurrentFutPxQty = new IntOptimProperty(1, false, 1, 1000000, 1);

        public OptimProperty NumerVegaAtmVega = new OptimProperty(0, true, -1000000, 1000000, 1, 3);

        public OptimProperty TotalQtyOpenQty = new OptimProperty(0, true, 1, 10, 1, 0);

        public OptimProperty NumerThetaAtmTheta = new OptimProperty(0, true, -1000000, 1000000, 1, 3);

        public OptimProperty SinSerNumDelIntSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        public OptimProperty SsngiSimmSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        public OptimProperty NumGamAtmIntGamma = new OptimProperty(0, true, -1000000, 1000000, 1, 6);

        public BoolOptimProperty ZeroPnLShowNodes = new BoolOptimProperty(false, false);

        public OptimProperty ZeroPnLSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        // PROD-5353 - Схлопываем два контрола в один
        //public EnumOptimProperty IvAsksOptionType = new EnumOptimProperty(StrikeType.Any, false);
        //public EnumOptimProperty IvBidsOptionType = new EnumOptimProperty(StrikeType.Any, false);
        public EnumOptimProperty IvOptionType = new EnumOptimProperty(StrikeType.Any, false);

        public IntOptimProperty TradeAsksQty = new IntOptimProperty(10, false, 1, 1000000, 1);

        public OptimProperty TradeAsksWidthPx = new OptimProperty(100D, false, -10000000, 10000000, 10, 0);

        public IntOptimProperty TradeBidsQty = new IntOptimProperty(10, false, 1, 1000000, 1);

        public OptimProperty TradeBidsWidthPx = new OptimProperty(100D, false, -10000000, 10000000, 10, 0);

        public OptimProperty NumDeltaAtmIntDelta = new OptimProperty(0, true, -1000000, 1000000, 1, 6);

        public BoolOptimProperty NumDeltaAtmIntHedgeDelta = new BoolOptimProperty(false, false);

        public BoolOptimProperty NumDeltaAtmIntPrintDeltaInLog = new BoolOptimProperty(false, false);

        public BoolOptimProperty ManageSmilePaneApplyVisualSettings = new BoolOptimProperty(false, false);

        public OptimProperty ManageSmilePaneSigmaMult = new OptimProperty(5D, false, 0D, 1000000D, 1D, 1);

        public OptimProperty ManageSmilePaneXAxisDivisor = new OptimProperty(1000D, false, 1E-09D, 10000000D, 1D, 1);

        public OptimProperty ManageSmilePaneXAxisStep = new OptimProperty(5000D, false, 1E-09D, 10000000D, 1D, 1);

        //public OptimProperty ManagePosPaneSigmaMult = new OptimProperty(7D, false, 0D, 1000000D, 1D, 3);

        public OptimProperty ManagePosPaneXAxisDivisor = new OptimProperty(1000D, false, 1E-09D, 10000000D, 1D, 1);

        public OptimProperty ManagePosPaneXAxisStep = new OptimProperty(5000D, false, 1E-09D, 10000000D, 1D, 1);

        public BoolOptimProperty QuoteIvCancelAllLong = new BoolOptimProperty(false, false);

        public BoolOptimProperty QuoteIvCancelAllShort = new BoolOptimProperty(false, false);

        public BoolOptimProperty QuoteIvExecuteCommand = new BoolOptimProperty(false, false);

        public IntOptimProperty QuoteIvQty = new IntOptimProperty(0, false, -1000000, 1000000, 1);

        public OptimProperty QuoteIvShiftIvPct = new OptimProperty(0D, false, -10000000D, 10000000D, 0.2, 2);

        public IntOptimProperty QuoteIvShiftPrice = new IntOptimProperty(0, false, -10000000, 10000000, 1);

        public StringOptimProperty QuoteIvStrike = new StringOptimProperty("120000", false);

        public EnumOptimProperty QuoteIvOptionType = new EnumOptimProperty(StrikeType.Any, false);

        public OptimProperty TotalProfitDisplayValue = new OptimProperty(0, true, -1000000, 1000000, 1, 1);
        #endregion Fields - Optim properties

        /// <summary>
        /// Формат экспирации должен соблюдаться крайне строго: dd-MM-yyyy HH:mm
        /// Он должен совпадать с форматом TimeToExpiry.DateTimeFormat
        /// </summary>
        private string m_expDateStr = DefaultExpiration;

        /// <summary>
        /// \~english Base asset price (only to display at UI)
        /// \~russian Цена БА (только для отображения в интерфейсе)
        /// </summary>
        [ReadOnly(true)]
        [HelperName("Display Price", Constants.En)]
        [HelperName("Цена для интерфейса", Constants.Ru)]
        [Description("Цена БА (только для отображения в интерфейсе)")]
        [HelperDescription("Base asset price (only to display at UI)", Constants.En)]
        //[HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Default = DefaultPx, IsCalculable = true)]
        // ReSharper disable once InconsistentNaming
        public OptimProperty DisplayPrice = new OptimProperty(Double.Parse(DefaultPx, CultureInfo.InvariantCulture), true, Double.MinValue, Double.MaxValue, 1.0, 4);

        /// <summary>
        /// \~english Time to expiry (just to show it on ControlPane)
        /// \~russian Время до экспирации (для отображения в интерфейсе агента)
        /// </summary>
        [HelperName("Display Time", Constants.En)]
        [HelperName("Время для интерфейса", Constants.Ru)]
        [ReadOnly(true)]
        [Description("Время до экспирации (только для отображения на UI)")]
        [HelperDescription("Time to expiry (just to show it on ControlPane)", Language = Constants.En)]
        //[HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Default = "0.08", IsCalculable = true)]
        // ReSharper disable once InconsistentNaming
        public OptimProperty DisplayTime = new OptimProperty(30, true, Double.MinValue, Double.MaxValue, 1.0, 3);

        /// <summary>
        /// \~english Implied volatility ATM (just to show it on ControlPane)
        /// \~russian Подразумеваемая волатильность на-деньгах (для отображения в интерфейсе агента)
        /// </summary>
        [HelperName("Display IV", Constants.En)]
        [HelperName("Волатильность для интерфейса", Constants.Ru)]
        [ReadOnly(true)]
        [Description("Подразумеваемая волатильность на-деньгах (для отображения в интерфейсе агента)")]
        [HelperDescription("Implied volatility ATM (just to show it on ControlPane)", Language = Constants.En)]
        //[HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Default = "0.08", IsCalculable = true)]
        // ReSharper disable once InconsistentNaming
        public OptimProperty DisplayIvPct = new OptimProperty(30, true, Double.MinValue, Double.MaxValue, 1.0, 1);

        public OptimProperty DisplaySigmaPriceWidth = new OptimProperty(-1, true, Double.MinValue, Double.MaxValue, 1.0, 0);

        /// <summary>
        /// Дата экспирации (включая время суток) для режима FixedExpiry.
        /// Обязательно как ПОЛЕ класса!!! См. PROD-2558
        /// </summary>
        //[HelperName("Expiry", Constants.En)]
        //[HelperName("Экспирация", Constants.Ru)]
        //[Description("Дата экспирации (включая время суток) для режима FixedExpiry")]
        //[HelperDescription("Expiration datetime (including time of a day) for algorythm FixedExpiry", Language = Constants.En)]
        //[HandlerParameter(Default = DefaultExpiration)]
        // ReSharper disable once InconsistentNaming
        public StringOptimProperty Expiry = new StringOptimProperty(DefaultExpiration);

        public EnumOptimProperty TimeMode = new EnumOptimProperty(TimeRemainMode.RtsTradingTime);
        public EnumOptimProperty BasePriceMode = new EnumOptimProperty(BasePxMode.LastTrade);
        public EnumOptimProperty SmileMode = new EnumOptimProperty(SmileType.Market);

        public OptimProperty RiskFreeRatePct = new OptimProperty(0, false, Double.MinValue, Double.MaxValue, 1.0, 3);
        public OptimProperty StrikesStep = new OptimProperty(0, false, 0, double.MaxValue, 1.0, 3);

        #region Вспомогательные обработчики для заполнения Доски Опционов
        public OptionsBoardVolatility BoardVolatilityH = new OptionsBoardVolatility();
        public OptionsBoardPrice BoardPutPriceH = new OptionsBoardPrice();
        public OptionsBoardPrice BoardCallPriceH = new OptionsBoardPrice();
        public OptionsBoardNumericalDelta BoardNumericalPutDeltaH = new OptionsBoardNumericalDelta();
        public OptionsBoardNumericalDelta BoardNumericalCallDeltaH = new OptionsBoardNumericalDelta();
        public OptionsBoardNumericalGamma BoardNumericalPutGammaH = new OptionsBoardNumericalGamma();
        public OptionsBoardNumericalGamma BoardNumericalCallGammaH = new OptionsBoardNumericalGamma();
        public OptionsBoardNumericalTheta BoardNumericalPutThetaH = new OptionsBoardNumericalTheta();
        public OptionsBoardNumericalTheta BoardNumericalCallThetaH = new OptionsBoardNumericalTheta();
        public OptionsBoardNumericalVega BoardNumericalPutVegaH = new OptionsBoardNumericalVega();
        public OptionsBoardNumericalVega BoardNumericalCallVegaH = new OptionsBoardNumericalVega();

        public InteractiveSeries BoardVolatility;
        public InteractiveSeries BoardPutPrice, BoardCallPrice;
        public InteractiveSeries BoardNumericalPutDelta, BoardNumericalCallDelta;
        public InteractiveSeries BoardNumericalPutGamma, BoardNumericalCallGamma;
        public InteractiveSeries BoardNumericalPutTheta, BoardNumericalCallTheta;
        public InteractiveSeries BoardNumericalPutVega, BoardNumericalCallVega;
        #endregion Вспомогательные обработчики для заполнения Доски Опционов

        #region Public Methods and Operators
        public void Dispose()
        {
            m_currentFutPxH.Dispose();
            m_currentFutPx1H.Dispose();
            m_tradeAsksH.Dispose();
            m_tradeBidsH.Dispose();
            m_quoteIvH.Dispose();
        }

        /// <summary>
        /// Инициализирую вспомогательные обработчики для заполнения Доски Опционов
        /// </summary>
        private void InitializeBoardHandlers(IContext context)
        {
            // Initialize 'BoardVolatilityH' item
            BoardVolatilityH.Context = context;
            BoardVolatilityH.VariableId = "6760F6A8-C8DB-4527-8861-BD5940559EC4";
            BoardVolatilityH.OptionType = StrikeType.Any;
            BoardVolatilityH.ShowNodes = false;
            BoardVolatilityH.SigmaMult = 7;

            // Initialize 'BoardPutPriceH' item
            BoardPutPriceH.Context = context;
            BoardPutPriceH.VariableId = "16A2947F-DB42-41C9-A05D-2B0009A0C5B9";
            BoardPutPriceH.OptionType = StrikeType.Put;
            BoardPutPriceH.ShowNodes = false;
            BoardPutPriceH.SigmaMult = 7;
            // Initialize 'BoardCallPriceH' item
            BoardCallPriceH.Context = context;
            BoardCallPriceH.VariableId = "87787492-C695-4205-AB7A-6B3D0BD38DD8";
            BoardCallPriceH.OptionType = StrikeType.Call;
            BoardCallPriceH.ShowNodes = false;
            BoardCallPriceH.SigmaMult = 7;

            // Initialize 'BoardNumericalPutDeltaH' item
            BoardNumericalPutDeltaH.Context = context;
            BoardNumericalPutDeltaH.VariableId = "40B5503D-F1C9-424D-819C-EDB51333857B";
            BoardNumericalPutDeltaH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            BoardNumericalPutDeltaH.OptionType = StrikeType.Put;
            BoardNumericalPutDeltaH.ShowNodes = false;
            BoardNumericalPutDeltaH.SigmaMult = 7;
            // Initialize 'BoardNumericalCallDeltaH' item
            BoardNumericalCallDeltaH.Context = context;
            BoardNumericalCallDeltaH.VariableId = "3868CA2A-6412-43A5-B105-2E642F25EC65";
            BoardNumericalCallDeltaH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            BoardNumericalCallDeltaH.OptionType = StrikeType.Call;
            BoardNumericalCallDeltaH.ShowNodes = false;
            BoardNumericalCallDeltaH.SigmaMult = 7;
            // Initialize 'BoardNumericalPutGammaH' item
            BoardNumericalPutGammaH.Context = context;
            BoardNumericalPutGammaH.VariableId = "98C96689-CCE3-4450-9665-16EED13D233E";
            BoardNumericalPutGammaH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            BoardNumericalPutGammaH.OptionType = StrikeType.Put;
            BoardNumericalPutGammaH.FixedQty = (int)Constants.MillionMult;
            BoardNumericalPutGammaH.TooltipFormat = "######0.00";
            BoardNumericalPutGammaH.ShowNodes = false;
            BoardNumericalPutGammaH.SigmaMult = 7;
            // Initialize 'BoardNumericalCallGammaH' item
            BoardNumericalCallGammaH.Context = context;
            BoardNumericalCallGammaH.VariableId = "9138ED21-0347-43BC-82C0-014B13503A0A";
            BoardNumericalCallGammaH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            BoardNumericalCallGammaH.OptionType = StrikeType.Call;
            BoardNumericalCallGammaH.FixedQty = (int)Constants.MillionMult;
            BoardNumericalCallGammaH.TooltipFormat = "######0.00";
            BoardNumericalCallGammaH.ShowNodes = false;
            BoardNumericalCallGammaH.SigmaMult = 7;

            // Initialize 'BoardNumericalPutThetaH' item
            BoardNumericalPutThetaH.Context = context;
            BoardNumericalPutThetaH.VariableId = "FDF6DF83-A2B6-4D06-8428-66D1B6C285A7";
            BoardNumericalPutThetaH.GreekAlgo = NumericalGreekAlgo.FrozenSmile;
            BoardNumericalPutThetaH.OptionType = StrikeType.Put;
            BoardNumericalPutThetaH.ShowNodes = false;
            BoardNumericalPutThetaH.SigmaMult = 7;
            // Initialize 'BoardNumericalCallThetaH' item
            BoardNumericalCallThetaH.Context = context;
            BoardNumericalCallThetaH.VariableId = "7AF77A1A-118E-425B-B390-0E174FB86DE1";
            BoardNumericalCallThetaH.GreekAlgo = NumericalGreekAlgo.FrozenSmile;
            BoardNumericalCallThetaH.OptionType = StrikeType.Call;
            BoardNumericalCallThetaH.ShowNodes = false;
            BoardNumericalCallThetaH.SigmaMult = 7;

            // Initialize 'BoardNumericalPutVegaH' item
            BoardNumericalPutVegaH.Context = context;
            BoardNumericalPutVegaH.VariableId = "1FACE3A9-C09A-4F6B-9E56-30642D40D763";
            BoardNumericalPutVegaH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            BoardNumericalPutVegaH.OptionType = StrikeType.Put;
            BoardNumericalPutVegaH.ShowNodes = false;
            BoardNumericalPutVegaH.SigmaMult = 7;
            // Initialize 'BoardNumericalCallVegaH' item
            BoardNumericalCallVegaH.Context = context;
            BoardNumericalCallVegaH.VariableId = "BCBF44A6-2030-475D-B359-6C66A761E6D0";
            BoardNumericalCallVegaH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            BoardNumericalCallVegaH.OptionType = StrikeType.Call;
            BoardNumericalCallVegaH.ShowNodes = false;
            BoardNumericalCallVegaH.SigmaMult = 7;
        }

        public void Execute(IContext context, IOption option)
        {
            // Текущее значение даты экпирации
            m_expDateStr = Expiry.Value;

            int actualDecimals = 1;
            var optionUnderlyingAsset = option.UnderlyingAsset;
            {
                int futDecimals = (option != null) && (optionUnderlyingAsset != null) ? optionUnderlyingAsset.SecurityDescription.Decimals : actualDecimals;
                actualDecimals = Math.Abs(futDecimals);
                if ((option != null) && (optionUnderlyingAsset != null) && (optionUnderlyingAsset.Tick > 0))
                {
                    double futTick = optionUnderlyingAsset.Tick;
                    BasePxMode basePxMode2 = (BasePriceMode.Value != null) && (BasePriceMode.Value is BasePxMode) ?
                        (BasePxMode)BasePriceMode.Value : BasePxMode.LastTrade;
                    if ((basePxMode2 == BasePxMode.BidAskMidPoint) || (basePxMode2 == BasePxMode.TheorPxBased))
                    {
                        // В этих режимах нужно адекватно увеличить точность отображения
                        futTick /= 2;
                    }
                    int tickDecimals = optionUnderlyingAsset.SecurityDescription.DecimalsFromTick(futTick);

                    actualDecimals = Math.Max(Math.Abs(futDecimals), Math.Abs(tickDecimals));
                    //BasePriceDisplayPrice.NumberDecimalDigits = actualDecimals;
                }
            }

            // формат для количество купленных опционов
            var qtyFormat = optionUnderlyingAsset == null || optionUnderlyingAsset.LotTick >= 1
                                ? "### ##0"
                                : "### ##0." + new string('0',
                                      optionUnderlyingAsset.SecurityDescription.DecimalsFromTick(optionUnderlyingAsset
                                          .LotTick));

            // Форматирование профита в тултипах узлов делаю с учетом фактической точности инструмента
            MktProfileTooltipFormat.Value = ExpiryProfileTooltipFormat.Value = MktSimmProfileTooltipFormat.Value = "N" + actualDecimals;

            // PROD-5774 - Детектирую цветовую схему и делаю подмену если нужно
            /*var theme = AppUtils.CurrentTheme;
            if (theme == AppThemes.Light)
            {
                MarketProfileAnchorColor = AlphaColors.Magenta; // Якоря делаем сиреневыми: 0xFF00FF
                ModelProfileLineColor = 0x0F0F0F; // В светлой теме профиль будет почти черный
                PortfolioProfileAnchorColor = AlphaColors.Magenta; // Якоря делаем сиреневыми: 0xFF00FF
            }*/
            
            // =================================================
            // Windows
            // =================================================
            // Make 'SmileWin' window
            //IWindow smileWinWnd = context.AddWindow("SmileWin", "Smile");
            // Make 'PosWin' window
            IWindow posWinWnd = context.AddWindow("PosWin", RM.GetString("OptBoard.Win.PositionProfile") /* "Pos Profile" */);
            // =================================================
            // Panels
            // =================================================
            // Make 'SmilePane' pane
            string futName = (option != null) && (optionUnderlyingAsset != null) ? optionUnderlyingAsset.SecurityDescription.Name : "";
            var smilePanePane = context.CreateCanvasPane("SmilePane", RM.GetString("OptBoard.Pane.SmilePane") + ": " + futName + " - " + m_expDateStr);
            #region Configure 'SmilePane' pane
            {
                smilePanePane.Visible = true;
                smilePanePane.HideLegend = false;
                smilePanePane.Dockability = false;
                smilePanePane.ScaleStepPercent = 0D;
                smilePanePane.GraphIndentPercent = 5;
                // PROD-3901 - Всегда выставлять FeetToBorder2ByDefault = true
                smilePanePane.FeetToBorder2ByDefault = true;
                // PROD-3577 - Вообще не надо трогать Border1!
                //smilePanePane.BorderX1 = 60000D;
                //smilePanePane.BorderX2 = 110000D;
                //smilePanePane.BorderY1 = 0.2D;
                //smilePanePane.BorderY2 = 0.6D;
                smilePanePane.ValidBorderMode = ValidBorderMode.Fixed;
                smilePanePane.ValidBorderX1 = 0;
                smilePanePane.ValidBorderY1 = -0.05;

                // PROD-5740 - Поменьше дергаем настройки панелей
                //smilePanePane.XAxisStep = 5000D;
                //smilePanePane.XAxisDiviser = 1000D;
                smilePanePane.XAxisLineIsVisible = true;
                smilePanePane.XAxisLineColor = -1; // белый насколько понимаю
                smilePanePane.XAxisLineStyle = LineStyles.SOLID;
                smilePanePane.XAxisLinePosition = 0D;

                smilePanePane.YAxisLineIsVisible = true;
                smilePanePane.YAxisLineColor = -1; // белый насколько понимаю
                smilePanePane.YAxisLineStyle = LineStyles.SOLID;
                smilePanePane.YAxisLinePosition = 0D;
            }
            #endregion Configure 'SmilePane' pane

            // Make 'PosPane' pane
            var posPanePane = posWinWnd.CreateCanvasPane("PosPane", RM.GetString("OptBoard.Pane.PositionPane") + ": " + futName + " - " + m_expDateStr);
            #region Configure 'PosPane' pane
            {
                posPanePane.Visible = true;
                posPanePane.HideLegend = false;
                posPanePane.Dockability = false;
                posPanePane.ScaleStepPercent = 0D;
                posPanePane.GraphIndentPercent = 5;
                // PROD-3901 - Всегда выставлять FeetToBorder2ByDefault = true
                posPanePane.FeetToBorder2ByDefault = true;
                // PROD-3577 - Вообще не надо трогать Border1!
                //posPanePane.BorderX1 = 60000D;
                //posPanePane.BorderX2 = 110000D;
                //posPanePane.BorderY1 = null;
                //posPanePane.BorderY2 = null;
                posPanePane.ValidBorderMode = ValidBorderMode.Fixed;
                posPanePane.ValidBorderX1 = 0;

                // PROD-5740 - Поменьше дергаем настройки панелей
                //posPanePane.XAxisStep = 5000D;
                //posPanePane.XAxisDiviser = 1000D;
                posPanePane.XAxisLineIsVisible = true;
                posPanePane.XAxisLineColor = -1; // белый насколько понимаю
                posPanePane.XAxisLineStyle = LineStyles.SOLID;
                posPanePane.XAxisLinePosition = 0D;

                posPanePane.YAxisLineIsVisible = true;
                posPanePane.YAxisLineColor = -1; // белый насколько понимаю
                posPanePane.YAxisLineStyle = LineStyles.SOLID;
                posPanePane.YAxisLinePosition = 0D;

                // Точность правой шкалы настраиваю один раз. Остальные вызовы делаются по ошибке.
                posPanePane.YAxisPrecision = actualDecimals; // option.Decimals;
            }
            #endregion Configure 'PosPane' pane

            // =================================================
            // Control Panels
            // =================================================
            // Make 'VisualPane' control pane
            var visualPanePane = context.CreateControlPane("8ebc5645-9336-48c0-9ec4-a1382a821409", "VisualPane", RM.GetString("OptBoard.Pane.VisualSettings"));
            // Make 'MktPane' control pane
            //IControlPane mktPanePane = smileWinWnd.CreateControlPane("5e2e8590-27d1-4b57-8511-cac9ead95962", "MktPane");
            var mktPanePane = context.CreateControlPane("5e2e8590-27d1-4b57-8511-cac9ead95962", "MktPane", RM.GetString("OptBoard.Pane.MarketSettings"));
            // Make 'HedgeControlPane' control pane
            //IControlPane hedgeControlPanePane = smileWinWnd.CreateControlPane("40d69210-5c61-4a5b-93d4-5efa0d02c7f2", "HedgeControlPane");
            var hedgeControlPanePane = context.CreateControlPane("40d69210-5c61-4a5b-93d4-5efa0d02c7f2", "HedgeControlPane", RM.GetString("OptBoard.Pane.HedgeSettings"));
            // Make 'SmileControlPane' control pane
            //IControlPane smileControlPanePane = smileWinWnd.CreateControlPane("7146ef79-52c6-4093-aa85-6d28ea737c26", "SmileControlPane");
            var smileControlPanePane = context.CreateControlPane("7146ef79-52c6-4093-aa85-6d28ea737c26", "SmileControlPane", RM.GetString("OptBoard.Pane.TradeSettings"));
            // Make 'QuotePane' control pane
            var quotePanePane = context.CreateControlPane("e7db4c07-d516-407f-b99b-4003969e0493", "QuotePane", RM.GetString("OptBoard.Pane.QuoteSettings"));
            // Make 'PosControlPane' control pane
            var posControlPanePane = posWinWnd.CreateControlPane("6b8aeecf-1e7e-419c-9c36-6342e0962bf4", "PosControlPane", RM.GetString("OptBoard.Pane.PositionOverview"));
            // =================================================
            // DataGrid Panels
            // =================================================
            // Make 'DataGridPane' dataGrid pane
            var dataGridPaneDataGridPane = posWinWnd.CreateDataGridPane("DataGridPane", RM.GetString("OptBoard.Pane.Position"), 2, "N" + actualDecimals, "K", true, TextAlignment.Left, null, 1, "d", "Date", false, TextAlignment.Left, null);
            // Make 'PriceGrid' dataGrid pane
            var priceGridDataGridPane = posWinWnd.CreateDataGridPane("PriceGrid", RM.GetString("OptBoard.Pane.AveragePrices"), 2, "N" + actualDecimals, "K", true, TextAlignment.Left, null, 1, "d", "Date", false, TextAlignment.Left, null);

            // Initialize 'PosMan' item
            m_posManH.Context = context;
            m_posManH.VariableId = "74530095-d19f-489b-996e-70f55e76a79f";
            m_posManH.AgregatePositions = PosManAgregatePositions;
            m_posManH.BlockTrading = PosManBlockTrading;
            m_posManH.DropVirtualPos = PosManDropVirtualPos;
            m_posManH.ImportRealPos = false;
            m_posManH.UseGlobalCache = false;
            m_posManH.UseVirtualPositions = PosManUseVirtualPositions;
            IOption posMan;
            // Initialize 'NearOptions' item
            m_nearOptionsH.Context = context;
            m_nearOptionsH.ExpirationMode = ExpiryMode.FixedExpiry;
            m_nearOptionsH.Expiry = m_expDateStr;
            m_nearOptionsH.Number = 1;
            IOptionSeries nearOptions;
            // Initialize 'dT' item
            m_dTh.Context = context;
            m_dTh.VariableId = "7dd5f841-f878-49cd-912f-5cc802076dce";
            m_dTh.CurDateMode = CurrentDateMode.CurrentDate;
            //m_dTh.DistanceMode = TimeRemainMode.RtsTradingTime;
            TimeRemainMode tRemainMode =
                (TimeMode.Value != null) && (TimeMode.Value is TimeRemainMode) ?
                (TimeRemainMode)TimeMode.Value : TimeRemainMode.RtsTradingTime;
            m_dTh.DistanceMode = tRemainMode;
            m_dTh.ExpirationMode = ExpiryMode.FixedExpiry;
            m_dTh.Expiry = m_expDateStr;
            m_dTh.ExpiryTime = DtExpiryTime;
            m_dTh.FixedDate = m_expDateStr;
            m_dTh.SeriesIndex = 1;
            m_dTh.Time = DtTime;
            m_dTh.UseDays = false;
            // Make 'dT' item data
            IList<double> dT = context.GetData("dT",
                new[]
                {
                    m_dTh.CurDateMode.ToString(), 
                    m_dTh.DistanceMode.ToString(), 
                    m_dTh.ExpirationMode.ToString(), 
                    m_dTh.Expiry, 
                    m_dTh.ExpiryTime, 
                    m_dTh.FixedDate, 
                    m_dTh.SeriesIndex.ToString(), 
                    m_dTh.Time.ToString(), 
                    m_dTh.UseDays.ToString(), 
                    "76e4b545-046c-4280-a4c5-eeea0fe2d168"
                }, () => m_dTh.Execute(option));
            // Initialize 'WrapDT' item
            m_wrapDtH.Coef = 1D;
            // Make 'WrapDT' item data
            IList<double> wrapDt = context.GetData("WrapDT",
                new[]
                {
                    m_dTh.CurDateMode.ToString(), 
                    m_dTh.DistanceMode.ToString(), 
                    m_dTh.ExpirationMode.ToString(), 
                    m_dTh.Expiry, 
                    m_dTh.ExpiryTime, 
                    m_dTh.FixedDate, 
                    m_dTh.SeriesIndex.ToString(), 
                    m_dTh.Time.ToString(), 
                    m_dTh.UseDays.ToString(), 
                    m_wrapDtH.Coef.ToString(CultureInfo.InvariantCulture), 
                    "76e4b545-046c-4280-a4c5-eeea0fe2d168"
                }, () => m_wrapDtH.Execute(dT));

            // Initialize 'BasePrice' item
            m_basePriceH.Context = context;
            m_basePriceH.VariableId = "6d3626a2-ab61-418c-879f-2b64d6888a4c";
            m_basePriceH.DisplayPrice = BasePriceDisplayPrice;
            m_basePriceH.DisplayUnits = FixedValueMode.AsIs;
            m_basePriceH.FixedPx = 120000D;
            //m_basePriceH.PxMode = BasePxMode.LastTrade;
            BasePxMode basePxMode =
                (BasePriceMode.Value != null) && (BasePriceMode.Value is BasePxMode) ?
                (BasePxMode)BasePriceMode.Value : BasePxMode.LastTrade;
            m_basePriceH.PxMode = basePxMode;
            m_basePriceH.RepeatLastPx = BasePriceRepeatLastPx;
            double basePrice = Double.NaN;
            // Initialize 'WrapFutPx' item
            m_wrapFutPxH.Add = 0D;
            m_wrapFutPxH.Mult = 1D;
            double wrapFutPx;
            // Initialize 'exSmileRescaled' item
            m_exSmileRescaledH.Context = context;
            m_exSmileRescaledH.VariableId = "0cb10c59-691a-4d30-a54f-df9414420b06";
            m_exSmileRescaledH.ExpiryTime = m_dTh.ExpireDateTime.ToString("HH:mm"); //it was always "18:45";
            m_exSmileRescaledH.OptionType = StrikeType.Call;
            m_exSmileRescaledH.RescaleTime = true;
            m_exSmileRescaledH.ShowNodes = false;
            m_exSmileRescaledH.SigmaMult = ExSmileRescaledSigmaMult;
            InteractiveSeries exSmileRescaled;
            InteractiveSeries exSmileRescaledChart = null;
            // Initialize 'GlobalSmile' item
            m_globalSmileH.Context = context;
            m_globalSmileH.VariableId = "d6d6aca0-7bd1-4a24-a3b6-c9825b64a1f6";
            m_globalSmileH.FrozenSmileID = "FrozenSmile";
            m_globalSmileH.GenerateTails = true;
            m_globalSmileH.GlobalSmileID = "GlobalSmile0";
            m_globalSmileH.IvAtmPct = GlobalSmileIvAtmPct;
            m_globalSmileH.SetIvByHands = GlobalSmileSetIvByHands;
            m_globalSmileH.SetShapeByHands = GlobalSmileSetShapeByHands;
            m_globalSmileH.SetSlopeByHands = GlobalSmileSetSlopeByHands;
            m_globalSmileH.ShapePct = GlobalSmileShapePct;
            m_globalSmileH.ShowNodes = true;
            m_globalSmileH.SigmaMult = GlobalSmileSigmaMult;
            m_globalSmileH.SlopePct = GlobalSmileSlopePct;
            m_globalSmileH.UseLocalTemplate = false;
            InteractiveSeries globalSmile;
            InteractiveSeries globalSmileChart = null;
            // Initialize 'WrapSmile' item
            m_wrapSmileH.Context = context;
            m_wrapSmileH.VariableId = "1e4332f5-7c70-4d1f-930b-6d80b91db55e";
            m_wrapSmileH.OptPxMode = OptionPxMode.Mid;
            m_wrapSmileH.ShiftIvPct = 0D;
            m_wrapSmileH.SimmWeight = 0.5D;
            m_wrapSmileH.Transformation = SmileTransformation.None;
            InteractiveSeries wrapSmile;
            // Initialize 'HV' item
            var isGlobalTime = tRemainMode == TimeRemainMode.PlainCalendar;
            m_globalHvH.Context = context;
            m_globalHvH.VariableId = "e70c61f4-cbff-4cf3-811c-1b43e65f2539";
            m_globalHvH.AnnualizingMultiplier = isGlobalTime ? 725D : 500D;
            m_globalHvH.Period = isGlobalTime ? 1440 : 990;
            m_globalHvH.RepeatLastHv = true;
            m_globalHvH.Timeframe = 60;
            m_globalHvH.UseAllData = isGlobalTime;
            // Make 'HV' item data
            Dictionary<DateTime, double> hvSigmas = null;
            // Тестирую наличие данных в глобальном кеше БЕЗ ВЫВОДА СООБЩЕНИЙ ОБ ОШИБКАХ
            string cashKey = HV.GetGlobalCashKey(optionUnderlyingAsset.Symbol, false,
                m_globalHvH.UseAllData, m_globalHvH.Timeframe, m_globalHvH.AnnualizingMultiplier, m_globalHvH.Period);
            try
            {
                object globalObj = context.LoadGlobalObject(cashKey, true);
                hvSigmas = globalObj as Dictionary<DateTime, double>;
                // PROD-3970 - 'Важный' объект
                if (hvSigmas == null)
                {
                    var container = globalObj as NotClearableContainer;
                    if ((container != null) && (container.Content != null))
                        hvSigmas = container.Content as Dictionary<DateTime, double>;
                }
            }
            catch (Exception ex)
            {
                string msg = String.Format("[{0}.PrepareData] {1} when loading 'hvSigmas' from global cache. cashKey: {2}; Message: {3}\r\n\r\n{4}",
                    GetType().Name, ex.GetType().FullName, cashKey, ex.Message, ex);
                //context.Log(msg, NotificationType.Alert, true);
                hvSigmas = null;
            }
            IList<double> hvData = null;
            if (hvSigmas != null)
            {
                hvData = context.GetData("HV",
                    new string[]
                    {
                        m_globalHvH.AnnualizingMultiplier.ToString(CultureInfo.InvariantCulture), 
                        m_globalHvH.Period.ToString(), 
                        m_globalHvH.RepeatLastHv.ToString(), 
                        m_globalHvH.Timeframe.ToString(), 
                        m_globalHvH.UseAllData.ToString(), 
                        "d0a486d3-faea-4a87-ae77-72ceee564e60"
                    }, () => m_globalHvH.Execute(option));
            }
            // Initialize 'MktProfile' item
            m_mktProfileH.Context = context;
            m_mktProfileH.VariableId = "f014b793-0bf2-448b-b4f5-b019c4968697";
            m_mktProfileH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            m_mktProfileH.NodesCount = MktProfileNodesCount; // not used
            m_mktProfileH.ShowNodes = true;
            m_mktProfileH.SigmaMult = MktProfileSigmaMult;
            m_mktProfileH.TooltipFormat = MktProfileTooltipFormat;
            InteractiveSeries mktProfile;
            InteractiveSeries mktProfileChart = null;
            // Initialize 'PortfolioProfile' item
            m_portfolioProfileH.Context = context;
            m_portfolioProfileH.VariableId = "4E6F5056-A0F2-49C5-9DD2-0A8B14F2E9D8";
            m_portfolioProfileH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            m_portfolioProfileH.NodesCount = MktProfileNodesCount; // not used
            m_portfolioProfileH.ShowNodes = true;
            m_portfolioProfileH.SigmaMult = MktProfileSigmaMult;
            m_portfolioProfileH.TooltipFormat = MktProfileTooltipFormat;
            InteractiveSeries portfolioProfile;
            InteractiveSeries portfolioProfileChart = null;
            // Initialize 'blackScholseSmile' item
            m_blackScholseSmileH.Context = context;
            m_blackScholseSmileH.VariableId = "4e19206c-a926-4298-82a4-7aa514d8cace";
            m_blackScholseSmileH.ShowNodes = false;
            m_blackScholseSmileH.SigmaMult = 7D;
            m_blackScholseSmileH.Label = "HV";
            InteractiveSeries blackScholseSmile = null;
            InteractiveSeries blackScholseSmileChart = null;
            // Initialize 'ZeroDT' item
            m_zeroDtH.Context = context;
            m_zeroDtH.VariableId = "24075319-43b4-4900-8c3e-05bade631eb4";
            m_zeroDtH.DisplayUnits = FixedValueMode.AsIs;
            m_zeroDtH.MinValue = 1E-13D;
            m_zeroDtH.Value = 1E-12D;
            double zeroDt;
            // Initialize 'ExpiryProfile' item
            m_expiryProfileH.Context = context;
            m_expiryProfileH.VariableId = "68b951db-e1c6-4e4d-85b2-c16149fb85e3";
            m_expiryProfileH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            m_expiryProfileH.NodesCount = ExpiryProfileNodesCount;
            m_expiryProfileH.ShowNodes = ExpiryProfileShowNodes;
            m_expiryProfileH.SigmaMult = ExpiryProfileSigmaMult;
            m_expiryProfileH.TooltipFormat = ExpiryProfileTooltipFormat;
            InteractiveSeries expiryProfile;
            InteractiveSeries expiryProfileChart = null;
            // Initialize 'QtyCalls' item
            m_qtyCallsH.Context = context;
            m_qtyCallsH.VariableId = "258f0e41-b379-48d6-b94a-fdf4c58ef859";
            m_qtyCallsH.CountFutures = false;
            m_qtyCallsH.OptionType = StrikeType.Call;
            m_qtyCallsH.ShowNodes = false;
            m_qtyCallsH.SigmaMult = 7D;
            m_qtyCallsH.TooltipFormat = QtyCallsTooltipFormat;
            InteractiveSeries qtyCalls;
            InteractiveSeries qtyCallsChart = null;
            // Initialize 'QtyPuts' item
            m_qtyPutsH.Context = context;
            m_qtyPutsH.VariableId = "ddb943f3-1e2b-42b6-bf21-7396aacdc044";
            m_qtyPutsH.CountFutures = false;
            m_qtyPutsH.OptionType = StrikeType.Put;
            m_qtyPutsH.ShowNodes = false;
            m_qtyPutsH.SigmaMult = 7D;
            m_qtyPutsH.TooltipFormat = QtyPutsTooltipFormat;
            InteractiveSeries qtyPuts;
            InteractiveSeries qtyPutsChart = null;
            // Initialize 'QtyTotal' item
            m_qtyTotalH.Context = context;
            m_qtyTotalH.VariableId = "53b88146-ef5f-4b3a-ba5e-b565eace6c45";
            m_qtyTotalH.CountFutures = true;
            m_qtyTotalH.OptionType = StrikeType.Any;
            m_qtyTotalH.ShowNodes = false;
            m_qtyTotalH.SigmaMult = 7D;
            m_qtyTotalH.TooltipFormat = QtyTotalTooltipFormat;
            InteractiveSeries qtyTotal;
            InteractiveSeries qtyTotalChart = null;
            // Initialize 'TotalProfit' item
            m_totalProfitH.Context = context;
            m_totalProfitH.VariableId = "e2d672d8-f0e5-4001-a99b-6709ffd9f21c";
            m_totalProfitH.PrintProfitInLog = false;
            m_totalProfitH.Profit = TotalProfitDisplayValue;
            m_totalProfitH.ProfitAlgo = TotalProfitAlgo.AllPositions;
            double totalProfit = 0;
            // Initialize 'HedgeSmile' item
            m_hedgeSmileH.Context = context;
            m_hedgeSmileH.VariableId = "00cf523d-4906-4d30-a133-f403625775f2";
            m_hedgeSmileH.FrozenSmileID = "FrozenSmile";
            m_hedgeSmileH.GenerateTails = true;
            m_hedgeSmileH.GlobalSmileID = "GlobalSmile0";
            m_hedgeSmileH.IvAtmPct = HedgeSmileIvAtmPct;
            m_hedgeSmileH.SetIvByHands = HedgeSmileSetIvByHands;
            m_hedgeSmileH.SetShapeByHands = true;
            m_hedgeSmileH.SetSlopeByHands = true;
            // [2016-10-21] PROD-4340 - Другой алгоритм построения модельной улыбки
            m_hedgeSmileH.SlopePct = HedgeSmileSlopePct; // GlobalSmileSlopePct;
            // [2016-03-31] PROD-3345 - Фиксирую Shape == 0
            m_hedgeSmileH.ShapePct = HedgeSmileShapePct; // GlobalSmileShapePct;
            m_hedgeSmileH.ShowNodes = false;
            m_hedgeSmileH.SigmaMult = HedgeSmileSigmaMult;
            m_hedgeSmileH.UseLocalTemplate = false;
            InteractiveSeries hedgeSmile;
            // Initialize 'SimmSmile' item
            m_simmSmileH.Context = context;
            m_simmSmileH.VariableId = "5fac9f3b-ef65-44d4-8b76-337494efa496";
            m_simmSmileH.OptPxMode = OptionPxMode.Mid;
            m_simmSmileH.ShiftIvPct = 0D;
            m_simmSmileH.SimmWeight = SimmWeight; //0.5D;
            // [2016-10-21] PROD-4340 - Другой алгоритм построения модельной улыбки
            m_simmSmileH.Transformation = SmileTransformation.None; // SmileTransformation.LogSimmetrise;
            InteractiveSeries simmSmile;
            InteractiveSeries simmSmileChart = null;

            // [2017-10-04] PROD-5352 - Портфельная улыбка
            // Initialize 'PortfolioSmile' item
            m_portfolioSmileH.Context = context;
            m_portfolioSmileH.VariableId = "22B553D5-EBA0-4B55-8353-2DBF4C1322AC";
            m_portfolioSmileH.FrozenSmileID = "FrozenSmile";
            m_portfolioSmileH.GenerateTails = true;
            m_portfolioSmileH.GlobalSmileID = "GlobalSmile0";
            m_portfolioSmileH.SetIvByHands = PortfolioSmileSetIvByHands;
            m_portfolioSmileH.SetSlopeByHands = PortfolioSmileSetSlopeByHands;
            m_portfolioSmileH.SetShapeByHands = true; // Форма всегда руками задается PortfolioSmileSetShapeByHands;
            m_portfolioSmileH.IvAtmPct = PortfolioSmileIvAtmPct;
            m_portfolioSmileH.SlopePct = PortfolioSmileSlopePct;
            m_portfolioSmileH.ShapePct = PortfolioSmileShapePct;
            m_portfolioSmileH.ShowNodes = false;
            m_portfolioSmileH.SigmaMult = GlobalSmileSigmaMult;
            m_portfolioSmileH.UseLocalTemplate = false;
            InteractiveSeries portfolioSmile;
            InteractiveSeries portfolioSmileChart = null;

            // Initialize 'MktSimmProfile' item
            m_mktSimmProfileH.Context = context;
            m_mktSimmProfileH.VariableId = "f014b793-0bf2-448b-b4f5-b019c4968697";
            m_mktSimmProfileH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            m_mktSimmProfileH.NodesCount = MktSimmProfileNodesCount;
            m_mktSimmProfileH.ShowNodes = true;
            m_mktSimmProfileH.SigmaMult = MktSimmProfileSigmaMult;
            m_mktSimmProfileH.TooltipFormat = MktSimmProfileTooltipFormat;
            InteractiveSeries mktSimmProfile;
            InteractiveSeries mktSimmProfileChart = null;
            // Initialize 'ssndiSimm' item
            m_ssndiSimmH.Context = context;
            m_ssndiSimmH.VariableId = "d610e17c-3f8b-4adc-bd3c-3626cd806126";
            m_ssndiSimmH.ShowNodes = true;
            m_ssndiSimmH.SigmaMult = SsndiSimmSigmaMult;
            m_ssndiSimmH.TooltipFormat = "0.000";
            InteractiveSeries ssndiSimm;
            // Initialize 'ndaiSimm' item
            m_ndaiSimmH.Context = context;
            m_ndaiSimmH.VariableId = "46c9b139-ba2f-4095-8590-57f4dbe705df";
            m_ndaiSimmH.Delta = NdaiSimmDelta;
            m_ndaiSimmH.HedgeDelta = NdaiSimmHedgeDelta;
            m_ndaiSimmH.PrintDeltaInLog = NdaiSimmPrintDeltaInLog;
            double ndaiSimm = 0;
            // Initialize 'AutoHedge' item
            m_autoHedgeH.Context = context;
            m_autoHedgeH.VariableId = "82f1342b-32e4-4434-98bb-3aedeceb601c";
            m_autoHedgeH.BuyShift = AutoHedgeBuyShift;
            m_autoHedgeH.SellShift = AutoHedgeSellShift;
            m_autoHedgeH.BuyPrice = AutoHedgeBuyPrice;
            m_autoHedgeH.SellPrice = AutoHedgeSellPrice;
            m_autoHedgeH.DownDelta = AutoHedgeDownDelta;
            m_autoHedgeH.HedgeDelta = AutoHedgeHedgeDelta;
            m_autoHedgeH.MinPeriod = AutoHedgeMinPeriod;
            m_autoHedgeH.SensitivityPct = AutoHedgeSensitivityPct;
            m_autoHedgeH.TargetDelta = AutoHedgeTargetDelta;
            m_autoHedgeH.UpDelta = AutoHedgeUpDelta;
            double autoHedge = 0;
            // Initialize 'CurrentFutPx' item
            m_currentFutPxH.Context = context;
            m_currentFutPxH.VariableId = "2bda62da-a3ea-417d-b333-929e7757a09f";
            m_currentFutPxH.MinHeight = 0.03D;
            m_currentFutPxH.OffsetPct = 10D;
            m_currentFutPxH.Qty = CurrentFutPxQty;
            InteractiveSeries currentFutPx;
            InteractiveSeries currentFutPxChart = null;
            // Initialize 'CurrentFutPx1' item
            m_currentFutPx1H.Context = context;
            m_currentFutPx1H.VariableId = "fa22041f-e842-47f4-ac0d-3a75a621e529";
            m_currentFutPx1H.MinHeight = 100D;
            m_currentFutPx1H.OffsetPct = 10D;
            m_currentFutPx1H.Qty = CurrentFutPxQty;
            m_currentFutPx1H.TooltipFormat = "### ### ##0.###";
            InteractiveSeries currentFutPx1;
            InteractiveSeries currentFutPx1Chart = null;
            // Initialize 'NumerVegaATM' item
            m_numerVegaAtmH.Context = context;
            m_numerVegaAtmH.VariableId = "142a4b0b-3b89-4cb3-9801-07897c970190";
            m_numerVegaAtmH.GreekAlgo = NumericalGreekAlgo.ShiftingSmile;
            m_numerVegaAtmH.SigmaStep = 0.0001D;
            m_numerVegaAtmH.Vega = NumerVegaAtmVega;
            double numerVegaAtm = 0;
            // Initialize 'OptionBase2' item
            ISecurity optionBase2;
            // Initialize 'TotalQty' item
            m_totalQtyH.Context = context;
            m_totalQtyH.VariableId = "1156553f-7842-4858-9799-839ab55d4b2b";
            m_totalQtyH.OpenQty = TotalQtyOpenQty;
            double totalQty = 0;
            // Initialize 'NumerThetaATM' item
            m_numerThetaAtmH.Context = context;
            m_numerThetaAtmH.VariableId = "bd0476cb-0b20-47c3-aedd-0af02efd36bd";
            m_numerThetaAtmH.GreekAlgo = NumericalGreekAlgo.FrozenSmile;
            m_numerThetaAtmH.Theta = NumerThetaAtmTheta;
            m_numerThetaAtmH.TStep = 1E-05D;
            // Локальная переменная tRemainMode заполняется выше при инициализации m_dTh
            m_numerThetaAtmH.DistanceMode = tRemainMode;
            double numerThetaAtm = 0;
            // Initialize 'SinSerNumDelInt' item
            m_sinSerNumDelIntH.Context = context;
            m_sinSerNumDelIntH.VariableId = "d610e17c-3f8b-4adc-bd3c-3626cd806126";
            m_sinSerNumDelIntH.ShowNodes = true;
            m_sinSerNumDelIntH.SigmaMult = SinSerNumDelIntSigmaMult;
            m_sinSerNumDelIntH.TooltipFormat = "0.000";
            InteractiveSeries sinSerNumDelInt;
            // Initialize 'ssngiSimm' item
            m_ssngiSimmH.Context = context;
            m_ssngiSimmH.VariableId = "a87b60a7-76b9-4d5d-8b99-7856a0617eac";
            m_ssngiSimmH.ShowNodes = true;
            m_ssngiSimmH.SigmaMult = SsngiSimmSigmaMult;
            m_ssngiSimmH.TooltipFormat = "0.0000000";
            InteractiveSeries ssngiSimm;
            // Initialize 'NumGamATMInt' item
            m_numGamAtmIntH.Context = context;
            m_numGamAtmIntH.VariableId = "5a6de5b6-7047-42dc-af6d-4ca00ed75ffa";
            m_numGamAtmIntH.Gamma = NumGamAtmIntGamma;
            double numGamAtmInt = 0;
            // Initialize 'ZeroPnL' item
            m_zeroPnLh.Context = context;
            m_zeroPnLh.VariableId = "e9184ea0-b0de-4d4f-8004-53c7810c10c3";
            m_zeroPnLh.ShowNodes = ZeroPnLShowNodes;
            m_zeroPnLh.SigmaMult = ZeroPnLSigmaMult;
            m_zeroPnLh.SigmaPct = 30D;
            m_zeroPnLh.ValuePct = 0D;
            m_zeroPnLh.ShowEdgeLabels = false;
            InteractiveSeries zeroPnl;
            InteractiveSeries zeroPnlChart = null;
            // Initialize 'IvAsks' item
            m_ivAsksH.Context = context;
            m_ivAsksH.VariableId = "92cd5616-549f-4ff3-9a4c-6201f85f349c";
            m_ivAsksH.MaxSigmaPct = 200D;
            m_ivAsksH.MaxStrike = 1000000D;
            m_ivAsksH.MinStrike = 1e-9;
            m_ivAsksH.OptionType = (StrikeType)IvOptionType.Value; // (StrikeType)IvAsksOptionType.Value; // StrikeType.Any;
            m_ivAsksH.OptPxMode = OptionPxMode.Ask;
            m_ivAsksH.ShiftAsk = 0D;
            m_ivAsksH.ShiftBid = 0D;
            m_ivAsksH.ShowNodes = true;
            m_ivAsksH.StrikeStep = 2500D;
            InteractiveSeries ivAsks;
            // Initialize 'IvBids' item
            m_ivBidsH.Context = context;
            m_ivBidsH.VariableId = "948ffcfb-08b7-411f-a240-4aae9baa7d87";
            m_ivBidsH.MaxSigmaPct = 200D;
            m_ivBidsH.MaxStrike = 1000000D;
            m_ivBidsH.MinStrike = 1e-9;
            m_ivBidsH.OptionType = (StrikeType)IvOptionType.Value; // (StrikeType)IvBidsOptionType.Value; // StrikeType.Any;
            m_ivBidsH.OptPxMode = OptionPxMode.Bid;
            m_ivBidsH.ShiftAsk = 0D;
            m_ivBidsH.ShiftBid = 0D;
            m_ivBidsH.ShowNodes = true;
            m_ivBidsH.StrikeStep = 2500D;
            InteractiveSeries ivBids;
            // Initialize 'TradeAsks' item
            m_tradeAsksH.Context = context;
            m_tradeAsksH.VariableId = "1ea8f723-fd0d-4b97-a94c-28f7b48b9e40";
            m_tradeAsksH.OptionType = StrikeType.Any;
            m_tradeAsksH.OptPxMode = OptionPxMode.Ask;
            m_tradeAsksH.OutletDistance = 0.02D;
            m_tradeAsksH.OutletSize = 14D;
            m_tradeAsksH.Qty = TradeAsksQty;
            m_tradeAsksH.WidthPx = TradeAsksWidthPx;
            InteractiveSeries tradeAsks;
            InteractiveSeries tradeAsksChart = null;
            // Initialize 'TradeBids' item
            m_tradeBidsH.Context = context;
            m_tradeBidsH.VariableId = "6779e7e6-1a7b-456e-bad8-e01f592fa939";
            m_tradeBidsH.OptionType = StrikeType.Any;
            m_tradeBidsH.OptPxMode = OptionPxMode.Bid;
            m_tradeBidsH.OutletDistance = 0.02D;
            m_tradeBidsH.OutletSize = 14D;
            m_tradeBidsH.Qty = TradeBidsQty;
            m_tradeBidsH.WidthPx = TradeBidsWidthPx;
            InteractiveSeries tradeBids;
            InteractiveSeries tradeBidsChart = null;
            // Initialize 'NumDeltaATMInt' item
            m_numDeltaAtmIntH.Context = context;
            m_numDeltaAtmIntH.VariableId = "b77c8226-c66b-4caf-8ef2-115fd600b047";
            m_numDeltaAtmIntH.Delta = NumDeltaAtmIntDelta;
            m_numDeltaAtmIntH.HedgeDelta = NumDeltaAtmIntHedgeDelta;
            m_numDeltaAtmIntH.PrintDeltaInLog = NumDeltaAtmIntPrintDeltaInLog;
            double numDeltaAtmInt = 0;
            // Initialize 'LongCallPxs' item
            m_longCallPxsH.Context = context;
            m_longCallPxsH.VariableId = "9dedd875-5963-4ef2-99e9-c39a1b71f470";
            m_longCallPxsH.CountFutures = true;
            m_longCallPxsH.LongPositions = true;
            m_longCallPxsH.OptionType = StrikeType.Call;
            m_longCallPxsH.ShowNodes = false;
            m_longCallPxsH.SigmaMult = 7D;
            m_longCallPxsH.TooltipFormat = "0";
            InteractiveSeries longCallPxs;
            InteractiveSeries longCallPxsChart = null;
            // Initialize 'LongPutPxs' item
            m_longPutPxsH.Context = context;
            m_longPutPxsH.VariableId = "e1e6ba49-22bc-4b11-bc48-9d2b84633206";
            m_longPutPxsH.CountFutures = false;
            m_longPutPxsH.LongPositions = true;
            m_longPutPxsH.OptionType = StrikeType.Put;
            m_longPutPxsH.ShowNodes = false;
            m_longPutPxsH.SigmaMult = 7D;
            m_longPutPxsH.TooltipFormat = "0";
            InteractiveSeries longPutPxs;
            InteractiveSeries longPutPxsChart = null;
            // Initialize 'ShortPutPxs' item
            m_shortPutPxsH.Context = context;
            m_shortPutPxsH.VariableId = "80f56306-a013-4b51-8b59-808d3eef3996";
            m_shortPutPxsH.CountFutures = false;
            m_shortPutPxsH.LongPositions = false;
            m_shortPutPxsH.OptionType = StrikeType.Put;
            m_shortPutPxsH.ShowNodes = false;
            m_shortPutPxsH.SigmaMult = 7D;
            m_shortPutPxsH.TooltipFormat = "0";
            InteractiveSeries shortPutPxs;
            InteractiveSeries shortPutPxsChart = null;
            // Initialize 'ShortCallPxs' item
            m_shortCallPxsH.Context = context;
            m_shortCallPxsH.VariableId = "7c2fae0b-18ce-45c5-b685-df061451c323";
            m_shortCallPxsH.CountFutures = true;
            m_shortCallPxsH.LongPositions = false;
            m_shortCallPxsH.OptionType = StrikeType.Call;
            m_shortCallPxsH.ShowNodes = false;
            m_shortCallPxsH.SigmaMult = 7D;
            m_shortCallPxsH.TooltipFormat = "0";
            InteractiveSeries shortCallPxs;
            InteractiveSeries shortCallPxsChart = null;
            // Initialize 'LongCallQty' item
            m_longCallQtyH.Context = context;
            m_longCallQtyH.VariableId = "334E7353-D783-4C4D-BCE6-9E75AF11F9F9";
            m_longCallQtyH.CountQty = true;
            m_longCallQtyH.CountFutures = true;
            m_longCallQtyH.LongPositions = true;
            m_longCallQtyH.OptionType = StrikeType.Call;
            m_longCallQtyH.ShowNodes = false;
            m_longCallQtyH.SigmaMult = 7D;
            m_longCallQtyH.TooltipFormat = "0";
            InteractiveSeries longCallQty;
            InteractiveSeries longCallQtyChart = null;
            // Initialize 'LongPutQty' item
            m_longPutQtyH.Context = context;
            m_longPutQtyH.VariableId = "782F6CDB-5B5F-4553-8EC5-2FA3593E9071";
            m_longPutQtyH.CountQty = true;
            m_longPutQtyH.CountFutures = false;
            m_longPutQtyH.LongPositions = true;
            m_longPutQtyH.OptionType = StrikeType.Put;
            m_longPutQtyH.ShowNodes = false;
            m_longPutQtyH.SigmaMult = 7D;
            m_longPutQtyH.TooltipFormat = "0";
            InteractiveSeries longPutQty;
            InteractiveSeries longPutQtyChart = null;
            // Initialize 'ShortPutQty' item
            m_shortPutQtyH.Context = context;
            m_shortPutQtyH.VariableId = "42212AAB-1D36-4CD6-9527-171B46B4232E";
            m_shortPutQtyH.CountQty = true;
            m_shortPutQtyH.CountFutures = false;
            m_shortPutQtyH.LongPositions = false;
            m_shortPutQtyH.OptionType = StrikeType.Put;
            m_shortPutQtyH.ShowNodes = false;
            m_shortPutQtyH.SigmaMult = 7D;
            m_shortPutQtyH.TooltipFormat = "0";
            InteractiveSeries shortPutQty;
            InteractiveSeries shortPutQtyChart = null;
            // Initialize 'ShortCallQty' item
            m_shortCallQtyH.Context = context;
            m_shortCallQtyH.VariableId = "17E0CBAE-2E8F-45BC-8690-5C62FC0EA3E7";
            m_shortCallQtyH.CountQty = true;
            m_shortCallQtyH.CountFutures = true;
            m_shortCallQtyH.LongPositions = false;
            m_shortCallQtyH.OptionType = StrikeType.Call;
            m_shortCallQtyH.ShowNodes = false;
            m_shortCallQtyH.SigmaMult = 7D;
            m_shortCallQtyH.TooltipFormat = "0";
            InteractiveSeries shortCallQty;
            InteractiveSeries shortCallQtyChart = null;
            // Initialize 'QuoteIv' item
            m_quoteIvH.Context = context;
            m_quoteIvH.VariableId = "7acddef7-d268-426b-8f4a-14e687a961f8";
            m_quoteIvH.CancelAllLong = QuoteIvCancelAllLong;
            m_quoteIvH.CancelAllShort = QuoteIvCancelAllShort;
            m_quoteIvH.ExecuteCommand = QuoteIvExecuteCommand;
            m_quoteIvH.OptionType = (StrikeType)QuoteIvOptionType.Value; //StrikeType.Any;
            m_quoteIvH.Qty = QuoteIvQty;
            m_quoteIvH.ShiftIvPct = QuoteIvShiftIvPct;
            m_quoteIvH.ShiftPrice = QuoteIvShiftPrice;
            m_quoteIvH.Strike = QuoteIvStrike;
            m_quoteIvH.StrikeStep = 0; //CentralStrike_StrikeStep;
            InteractiveSeries quoteIv;
            InteractiveSeries quoteIvChart = null;
            // Initialize 'ShowLongTargets' item
            m_showLongTargetsH.Context = context;
            m_showLongTargetsH.VariableId = "00de8ff4-7b4d-40f9-9c5e-96e568af48f8";
            m_showLongTargetsH.IsLong = true;
            InteractiveSeries showLongTargets;
            InteractiveSeries showLongTargetsChart = null;
            // Initialize 'ShowShortTargets' item
            m_showShortTargetsH.Context = context;
            m_showShortTargetsH.VariableId = "a36f483e-7d69-48d3-8cf1-b766ecd67374";
            m_showShortTargetsH.IsLong = false;
            InteractiveSeries showShortTargets;
            InteractiveSeries showShortTargetsChart = null;
            // =================================================
            // Handlers
            // =================================================
            // Initialize 'ManageSmilePane' item
            m_manageSmilePaneH.Context = context;
            m_manageSmilePaneH.VariableId = "759c1a68-dae7-4727-b07b-988311260fb7";
            m_manageSmilePaneH.ApplyVisualSettings = ManageSmilePaneApplyVisualSettings;
            m_manageSmilePaneH.ManageX = true;
            m_manageSmilePaneH.ManageXGridStep = true;
            m_manageSmilePaneH.ManageY = true;
            m_manageSmilePaneH.ShowNodes = false;
            m_manageSmilePaneH.SigmaMult = ManageSmilePaneSigmaMult;
            m_manageSmilePaneH.VerticalMultiplier = 1.8D;
            m_manageSmilePaneH.XAxisDivisor = ManageSmilePaneXAxisDivisor;
            m_manageSmilePaneH.XAxisStep = ManageSmilePaneXAxisStep;
            //m_manageSmilePaneH.ShouldWarmSecurities = false;
            // Initialize 'ManagePosPane' item
            m_managePosPaneH.Context = context;
            m_managePosPaneH.VariableId = "3a628034-2c13-4dee-afa7-849e1f35f60d";
            m_managePosPaneH.ApplyVisualSettings = ManageSmilePaneApplyVisualSettings;
            m_managePosPaneH.ManageX = true;
            m_managePosPaneH.ManageXGridStep = true;
            m_managePosPaneH.ManageY = false;
            m_managePosPaneH.ShowNodes = false;
            m_managePosPaneH.SigmaMult = ManageSmilePaneSigmaMult; //ManagePosPaneSigmaMult;
            m_managePosPaneH.VerticalMultiplier = 1.8D;
            m_managePosPaneH.XAxisDivisor = ManagePosPaneXAxisDivisor;
            m_managePosPaneH.XAxisStep = ManagePosPaneXAxisStep;

            // Инициализирую вспомогательные обработчики для заполнения Доски Опционов
            InitializeBoardHandlers(context);

            m_heartbeat.Context = context;
            m_heartbeat.VariableId = "01bcc969-4d8f-4ca5-9000-b4da95e624f4";
            m_heartbeat.OnlyAtTradingSession = true;
            // Начинаем работать только когда все уже полностью проинициализировано
            if ((option != null) && (optionUnderlyingAsset != null) && (optionUnderlyingAsset.IntervalInstance != null))
            {
                int intervalSec = optionUnderlyingAsset.IntervalInstance.ToSeconds();
                // Не менее 1 секунды?
                intervalSec = Math.Max(1, intervalSec);
                m_heartbeat.DelayMs = 1000 * intervalSec;
                // Информируем метроном об исполнении агента
                m_heartbeat.Execute(option);
            }

            // =================================================
            // Trading
            // =================================================
            int barsCount = optionUnderlyingAsset.Bars.Count;
            if ((context.IsLastBarUsed == false))
            {
                barsCount--;
            }

            for (int i = Math.Max(0, barsCount - 1); i < barsCount; i++)
            {
                posMan = m_posManH.Execute(option, i);
                basePrice = m_basePriceH.Execute(posMan, i);
                wrapFutPx = m_wrapFutPxH.Execute(basePrice, i);
                nearOptions = m_nearOptionsH.Execute(posMan, i);
                ivBids = m_ivBidsH.Execute(wrapFutPx, wrapDt[i], nearOptions, i);
                ivAsks = m_ivAsksH.Execute(wrapFutPx, wrapDt[i], nearOptions, i);

                exSmileRescaled = m_exSmileRescaledH.Execute(wrapFutPx, wrapDt[i], nearOptions, i);
                if ((exSmileRescaled == null) ||
                    (exSmileRescaled.ControlPoints.Count <= 0) ||
                    (exSmileRescaled.Tag == null))
                {
                    globalSmile = m_globalSmileH.Execute(wrapFutPx, wrapDt[i], ivAsks, nearOptions, i);
                    // [2017-10-04] PROD-5352 - Портфельная улыбка
                    portfolioSmile = m_portfolioSmileH.Execute(wrapFutPx, wrapDt[i], ivAsks, nearOptions, i);
                }
                else
                {
                    globalSmile = m_globalSmileH.Execute(wrapFutPx, wrapDt[i], exSmileRescaled, nearOptions, i);
                    // [2017-10-04] PROD-5352 - Портфельная улыбка
                    portfolioSmile = m_portfolioSmileH.Execute(wrapFutPx, wrapDt[i], exSmileRescaled, nearOptions, i);
                }

                wrapSmile = m_wrapSmileH.Execute(globalSmile, i);
                mktProfile = m_mktProfileH.Execute(wrapDt[i], wrapSmile, nearOptions, i);
                portfolioProfile = m_portfolioProfileH.Execute(wrapDt[i], portfolioSmile, nearOptions, i);
                zeroDt = m_zeroDtH.Execute(posMan, i);
                expiryProfile = m_expiryProfileH.Execute(zeroDt, wrapSmile, nearOptions, i);
                if ((hvData != null) && (i < hvData.Count))
                {
                    blackScholseSmile = m_blackScholseSmileH.Execute(wrapFutPx, wrapDt[i], hvData[i], i);
                    blackScholseSmileChart = blackScholseSmile;
                }
                qtyCalls = m_qtyCallsH.Execute(wrapDt[i], wrapSmile, nearOptions, i);
                qtyPuts = m_qtyPutsH.Execute(wrapDt[i], wrapSmile, nearOptions, i);
                qtyTotal = m_qtyTotalH.Execute(wrapDt[i], wrapSmile, nearOptions, i);
                // [2017-10-04] PROD-5352 - Портфельная улыбка
                //totalProfit = m_totalProfitH.Execute(mktProfile, i);
                totalProfit = m_totalProfitH.Execute(portfolioProfile, i);
                //hedgeSmile = m_hedgeSmileH.Execute(wrapFutPx, wrapDt[i], exSmileRescaled, nearOptions, i);
                // [2016-02-29] Модельная улыбка должна быть привязана к рыночной, а не к биржевой PROD-3165
                hedgeSmile = m_hedgeSmileH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                simmSmile = m_simmSmileH.Execute(hedgeSmile, i);
                mktSimmProfile = m_mktSimmProfileH.Execute(wrapDt[i], simmSmile, nearOptions, i);
                ssndiSimm = m_ssndiSimmH.Execute(mktSimmProfile, i);
                ndaiSimm = m_ndaiSimmH.Execute(ssndiSimm, nearOptions, i);
                autoHedge = m_autoHedgeH.Execute(wrapFutPx, ndaiSimm, nearOptions, i);
                currentFutPx = m_currentFutPxH.Execute(wrapFutPx, wrapSmile, nearOptions, i);
                currentFutPx1 = m_currentFutPx1H.Execute(wrapFutPx, mktProfile, nearOptions, i);
                quoteIv = m_quoteIvH.Execute(wrapSmile, nearOptions, i);
                showLongTargets = m_showLongTargetsH.Execute(wrapSmile, nearOptions, quoteIv, i);
                showShortTargets = m_showShortTargetsH.Execute(wrapSmile, nearOptions, quoteIv, i);
                numerVegaAtm = m_numerVegaAtmH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                optionBase2 = m_optionBase2H.Execute(posMan, i);
                totalQty = m_totalQtyH.Execute(optionBase2, i);
                numerThetaAtm = m_numerThetaAtmH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                ssngiSimm = m_ssngiSimmH.Execute(ssndiSimm, i);
                numGamAtmInt = m_numGamAtmIntH.Execute(ssngiSimm, i);
                zeroPnl = m_zeroPnLh.Execute(wrapFutPx, wrapDt[i], i);
                tradeAsks = m_tradeAsksH.Execute(ivAsks, wrapSmile, i);
                tradeBids = m_tradeBidsH.Execute(ivBids, wrapSmile, i);
                sinSerNumDelInt = m_sinSerNumDelIntH.Execute(mktProfile, i);
                numDeltaAtmInt = m_numDeltaAtmIntH.Execute(sinSerNumDelInt, nearOptions, i);
                //m_manageSmilePaneH.Execute(wrapFutPx, wrapDt[i], wrapSmile, smilePanePane, i);
                m_manageSmilePaneH.Execute(wrapFutPx, wrapDt[i], nearOptions, smilePanePane, i);
                //m_managePosPaneH.Execute(wrapFutPx, wrapDt[i], wrapSmile, posPanePane, i);
                m_managePosPaneH.Execute(wrapFutPx, wrapDt[i], nearOptions, posPanePane, i);
                longCallPxs = m_longCallPxsH.Execute(nearOptions, i);
                longPutPxs = m_longPutPxsH.Execute(nearOptions, i);
                shortPutPxs = m_shortPutPxsH.Execute(nearOptions, i);
                shortCallPxs = m_shortCallPxsH.Execute(nearOptions, i);
                longCallQty = m_longCallQtyH.Execute(nearOptions, i);
                longPutQty = m_longPutQtyH.Execute(nearOptions, i);
                shortPutQty = m_shortPutQtyH.Execute(nearOptions, i);
                shortCallQty = m_shortCallQtyH.Execute(nearOptions, i);
                mktProfileChart = mktProfile;
                // [2017-10-04] PROD-5352 - Портфельный профиль позиции
                portfolioProfileChart = portfolioProfile;
                expiryProfileChart = expiryProfile;
                qtyCallsChart = qtyCalls;
                qtyPutsChart = qtyPuts;
                qtyTotalChart = qtyTotal;
                currentFutPxChart = currentFutPx;
                currentFutPx1Chart = currentFutPx1;
                zeroPnlChart = zeroPnl;
                tradeAsksChart = tradeAsks;
                tradeBidsChart = tradeBids;
                exSmileRescaledChart = exSmileRescaled;
                globalSmileChart = globalSmile;
                mktSimmProfileChart = mktSimmProfile;
                simmSmileChart = simmSmile;
                // [2017-10-04] PROD-5352 - Портфельная улыбка
                portfolioSmileChart = portfolioSmile;
                longCallPxsChart = longCallPxs;
                longPutPxsChart = longPutPxs;
                shortPutPxsChart = shortPutPxs;
                shortCallPxsChart = shortCallPxs;
                longCallQtyChart = longCallQty;
                longPutQtyChart = longPutQty;
                shortPutQtyChart = shortPutQty;
                shortCallQtyChart = shortCallQty;
                quoteIvChart = quoteIv;
                showLongTargetsChart = showLongTargets;
                showShortTargetsChart = showShortTargets;

                #region Вспомогательные обработчики для заполнения Доски Опционов
                BoardVolatility = BoardVolatilityH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                BoardPutPrice = BoardPutPriceH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                BoardCallPrice = BoardCallPriceH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                BoardNumericalPutDelta = BoardNumericalPutDeltaH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                BoardNumericalCallDelta = BoardNumericalCallDeltaH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                BoardNumericalPutGamma = BoardNumericalPutGammaH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                BoardNumericalCallGamma = BoardNumericalCallGammaH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                BoardNumericalPutTheta = BoardNumericalPutThetaH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                BoardNumericalCallTheta = BoardNumericalCallThetaH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                BoardNumericalPutVega = BoardNumericalPutVegaH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);
                BoardNumericalCallVega = BoardNumericalCallVegaH.Execute(wrapFutPx, wrapDt[i], wrapSmile, nearOptions, i);

                context.StoreObject("BoardPutVolatility", BoardVolatility, false);
                context.StoreObject("BoardCallVolatility", BoardVolatility, false);
                context.StoreObject("BoardPutPrice", BoardPutPrice, false);
                context.StoreObject("BoardCallPrice", BoardCallPrice, false);
                context.StoreObject("BoardNumericalPutDelta", BoardNumericalPutDelta, false);
                context.StoreObject("BoardNumericalCallDelta", BoardNumericalCallDelta, false);
                context.StoreObject("BoardNumericalPutGamma", BoardNumericalPutGamma, false);
                context.StoreObject("BoardNumericalCallGamma", BoardNumericalCallGamma, false);
                context.StoreObject("BoardNumericalPutTheta", BoardNumericalPutTheta, false);
                context.StoreObject("BoardNumericalCallTheta", BoardNumericalCallTheta, false);
                context.StoreObject("BoardNumericalPutVega", BoardNumericalPutVega, false);
                context.StoreObject("BoardNumericalCallVega", BoardNumericalCallVega, false);
                #endregion Вспомогательные обработчики для заполнения Доски Опционов
            }

            // Отслеживаем ситуацию, когда происходит исполнение Доски без баров.
            // Возможно, это объяснит некоторые странные ситуации в момент открытия рынка?
            if (barsCount <= 0)
            {
                string tradeName = (context.Runtime?.TradeName ?? "NULL").Replace(Constants.HtmlDot, ".");
                string msg = String.Format(CultureInfo.InvariantCulture,
                    "[{0}.Execute] Execution without bar history. TradeName:'{1}';   barsCount:{2}",
                    GetType().Name, tradeName, barsCount);
                context.Log(msg, MessageType.Info, false);
            }

            // PROD-5740 - Если мы по какой-то причине не выставили нормальные границы панелям -- делаем это сейчас
            if (m_manageSmilePaneH.ManageX &&
                ((smilePanePane.Border2X1 == null) || (smilePanePane.Border2X2 == null)))
            {
                // Мы вроде как ДОЛЖНЫ настроить вьюпорт, но мы этого не сделали. Попробуем еще раз?
                Rect rect;
                string key = SetViewport.GetViewportCacheKey(m_manageSmilePaneH, m_expDateStr);
                var container = context.LoadObject(key) as NotClearableContainer<Rect>;
                string tradeName = (context.Runtime?.TradeName ?? "NULL").Replace(Constants.HtmlDot, ".");
                // Проверка на ApplyVisualSettings нужна, чтобы безусловно поменять видимую область при нажатии на кнопку в UI
                if ((container != null) /*&& (container.Content != null)*/ && (!m_manageSmilePaneH.ApplyVisualSettings) &&
                    DoubleUtil.IsPositive(container.Content.Width) && DoubleUtil.IsPositive(container.Content.Height))   // PROD-3901
                {
                    rect = container.Content;
                    string msg = String.Format(CultureInfo.InvariantCulture,
                        "[{0}.Execute] BACKUP EXECUTION PATH. TradeName:'{1}'; X:{2}; Width:{3}; Height:{4};   barsCount:{5}",
                        GetType().Name, tradeName, rect.X, rect.Width, rect.Height, barsCount);
                    context.Log(msg, MessageType.Info, false);

                    smilePanePane.Border2X1 = rect.X;
                    smilePanePane.Border2X2 = rect.X + rect.Width;
                }
                else
                {
                    string msg = String.Format(CultureInfo.InvariantCulture,
                        "[{0}.Execute] BAD EXECUTION PATH. TradeName:'{1}';   barsCount:{2}",
                        GetType().Name, tradeName, barsCount);
                    context.Log(msg, MessageType.Warning, false);
                }
            }

            //smilePanePane.UpdatePrecision(PaneSides.LEFT, 2);
            //smilePanePane.UpdatePrecision(PaneSides.RIGHT, 1);
            smilePanePane.YAxisPrecision = 1;
            //smilePanePane.SetByPercents(PaneSides.RIGHT, true);
            smilePanePane.YAxisByPercents = true;
            if (context.IsOptimization)
            {
                return;
            }
            // =================================================
            // Charts
            // =================================================
            // Make 'CurrentFutPx' chart
            IGraphListBase smilePanePaneCurrentFutPxChart = smilePanePane.AddList("SmilePane_pane_CurrentFutPx_chart",
                "CurrentFutPx (" + m_currentFutPxH.MinHeight + ", " + m_currentFutPxH.OffsetPct + ", " + m_currentFutPxH.Qty + ") " +
                "[" + option.Symbol + "]",
                currentFutPxChart,
                ListStyles.LINE, 16711680, LineStyles.SOLID, PaneSides.RIGHT,
                PointParameters.Empty,  // настройки якоря
                PointParameters.Empty,  // CP1
                PointParameters.Empty); // CP2
            smilePanePaneCurrentFutPxChart.AlternativeColor = 0;
            smilePanePaneCurrentFutPxChart.Thickness = 2;
            // Make 'TradeAsks' chart
            IGraphListBase smilePanePaneTradeAsksChart = smilePanePane.AddList("SmilePane_pane_TradeAsks_chart",
                "TradeAsks" + " (" + m_tradeAsksH.OptionType + ", " + m_tradeAsksH.OptPxMode
                + ", " + m_tradeAsksH.Qty + ", " + m_tradeAsksH.WidthPx
                + ", " + m_tradeAsksH.OutletDistance + ", " + m_tradeAsksH.OutletSize + ") "
                + "[" + option.Symbol + "]",
                tradeAsksChart,
                ListStyles.LINE, 9437184, LineStyles.SOLID, PaneSides.RIGHT,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Top, -1807350, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, true, // настройки якоря
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Top, -1, -8355712, -3942757, -16732080, DragableMode.None, Geometries.Circle, 14D, false, // CP1
                LabelHorizontalAlignment.Right, LabelVerticalAlignment.Center, 0, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false); // CP2
            smilePanePaneTradeAsksChart.AlternativeColor = 0;
            smilePanePaneTradeAsksChart.Opacity = 255;
            // Make 'TradeBids' chart
            IGraphListBase smilePanePaneTradeBidsChart = smilePanePane.AddList("SmilePane_pane_TradeBids_chart",
                "TradeBids" + " (" + m_tradeBidsH.OptionType + ", " + m_tradeBidsH.OptPxMode + ", " + m_tradeBidsH.Qty
                + ", " + m_tradeBidsH.WidthPx + ", " + m_tradeBidsH.OutletDistance + ", " + m_tradeBidsH.OutletSize + ") "
                + "[" + option.Symbol + "]",
                tradeBidsChart,
                ListStyles.LINE, 157, LineStyles.SOLID, PaneSides.RIGHT,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, -16748352, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, true, // настройки якоря
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, -1, -8355712, -3942757, -16732080, DragableMode.None, Geometries.Circle, 14D, false, // CP1
                LabelHorizontalAlignment.Right, LabelVerticalAlignment.Center, 0, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false); // CP2
            smilePanePaneTradeBidsChart.AlternativeColor = 0;
            smilePanePaneTradeBidsChart.Opacity = 255;
            // Make 'exSmileRescaled' chart
            IGraphListBase smilePanePaneExSmileRescaledChart = smilePanePane.AddList("SmilePane_pane_exSmileRescaled_chart",
                "exSmileRescaled" + " (" + m_exSmileRescaledH.RescaleTime + ", " + m_exSmileRescaledH.OptionType
                + ", " + m_exSmileRescaledH.ExpiryTime + ", " + m_exSmileRescaledH.SigmaMult + ", " + m_exSmileRescaledH.ShowNodes + ") "
                + "[" + option.Symbol + "]",
                exSmileRescaledChart,
                ListStyles.LINE, 5541589, LineStyles.SOLID, PaneSides.RIGHT,
                PointParameters.Empty,
                PointParameters.Empty,
                PointParameters.Empty);
            smilePanePaneExSmileRescaledChart.AlternativeColor = 0;
            // Make 'GlobalSmile' chart
            IGraphListBase smilePanePaneGlobalSmileChart = smilePanePane.AddList("SmilePane_pane_GlobalSmile_chart",
                "GlobalSmile" + " (" + m_globalSmileH.SetIvByHands + ", " + m_globalSmileH.SetSlopeByHands + ", " + m_globalSmileH.SetShapeByHands
                + ", " + m_globalSmileH.GenerateTails + ", " + m_globalSmileH.UseLocalTemplate + ", " + m_globalSmileH.IvAtmPct
                + ", " + m_globalSmileH.SlopePct + ", " + m_globalSmileH.ShapePct + ", " + m_globalSmileH.FrozenSmileID
                + ", " + m_globalSmileH.GlobalSmileID + ", " + m_globalSmileH.SigmaMult + ", " + m_globalSmileH.ShowNodes + ") "
                + "[" + option.Symbol + "]",
                globalSmileChart,
                ListStyles.LINE, MarketProfileLineColor /* 12603469 */, LineStyles.SOLID, PaneSides.RIGHT,
                new PointParameters(MarketProfileAnchorColor /* 16776960 */, DragableMode.None, Geometries.Circle, false),
                PointParameters.Empty,
                PointParameters.Empty);
            smilePanePaneGlobalSmileChart.AlternativeColor = 0;
            smilePanePaneGlobalSmileChart.Thickness = 2;
            // Make 'SimmSmile' chart
            IGraphListBase smilePanePaneSimmSmileChart = smilePanePane.AddList("SmilePane_pane_SimmSmile_chart",
                "SimmSmile" + " (" + m_simmSmileH.Transformation + ", " + m_simmSmileH.ShiftIvPct
                + ", " + m_simmSmileH.SimmWeight + ", " + m_simmSmileH.OptPxMode + ")"
                + " [" + option.Symbol + "]",
                simmSmileChart,
                ListStyles.LINE, ModelProfileLineColor /* 16777215 */, LineStyles.SOLID, PaneSides.RIGHT,
                new PointParameters(0, DragableMode.None, Geometries.Rect, true),
                PointParameters.Empty,
                PointParameters.Empty);
            smilePanePaneSimmSmileChart.AlternativeColor = ModelProfileLineColor /* 16777215 */; // White: 0xFFFFFF
            smilePanePaneSimmSmileChart.Thickness = 2;
            // Make 'Portfolio' chart
            IGraphListBase smilePanePanePortfolioChart = smilePanePane.AddList("SmilePane_pane_Portfolio_chart",
                (((((((((((("PortfolioSmile"
                + " (" + m_portfolioSmileH.SetIvByHands)
                + ", " + m_portfolioSmileH.SetSlopeByHands)
                + ", " + m_portfolioSmileH.SetShapeByHands)
                + ", " + m_portfolioSmileH.GenerateTails)
                + ", " + m_portfolioSmileH.UseLocalTemplate)
                + ", " + m_portfolioSmileH.IvAtmPct)
                + ", " + m_portfolioSmileH.SlopePct)
                + ", " + m_portfolioSmileH.ShapePct)
                + ", " + m_portfolioSmileH.FrozenSmileID)
                + ", " + m_portfolioSmileH.GlobalSmileID)
                + ", " + m_portfolioSmileH.SigmaMult)
                + ", " + m_portfolioSmileH.ShowNodes)
                + ") "
                + "[" + option.Symbol + "]",
                portfolioSmileChart,
                ListStyles.LINE, PortfolioProfileLineColor /* -16711936 */, LineStyles.SOLID, PaneSides.RIGHT,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, -8355712, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false,
                LabelHorizontalAlignment.Right, LabelVerticalAlignment.Center, -8355712, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false,
                LabelHorizontalAlignment.Right, LabelVerticalAlignment.Center, -8355712, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false);
            smilePanePanePortfolioChart.AlternativeColor = PortfolioProfileLineColor /* -16711936 */; // Green: 0x00FF00
            smilePanePanePortfolioChart.Thickness = 2;
            // Make 'blackScholseSmile' chart
            if ((hvData != null) && (blackScholseSmileChart != null) && (blackScholseSmileChart.ControlPoints.Count > 0))
            {
                IGraphListBase smilePanePaneBlacSchoSmilChart = smilePanePane.AddList("SmilePane_pane_BlacSchoSmil_chart",
                    "blackScholseSmile " + "(" + m_blackScholseSmileH.SigmaMult + ", " + m_blackScholseSmileH.ShowNodes + ") "
                    + "[" + option.Symbol + "]",
                    blackScholseSmileChart,
                    ListStyles.LINE, HvProfileLineColor /* Orange:16760832 */, LineStyles.DASH, PaneSides.RIGHT,
                    LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, -16384, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, true,
                    LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, -1, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false,
                    LabelHorizontalAlignment.Right, LabelVerticalAlignment.Center, 0, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false);
                smilePanePaneBlacSchoSmilChart.AlternativeColor = HvProfileLineColor; // Orange: 0xFFC000
                smilePanePaneBlacSchoSmilChart.Thickness = 3;
            }
            // Make 'QuoteIv' chart
            IGraphListBase smilePanePaneQuoteIvChart = smilePanePane.AddList("SmilePane_pane_QuoteIv_chart",
                "QuoteIv" + " (" + m_quoteIvH.Strike + ", " + m_quoteIvH.OptionType + ", " + m_quoteIvH.ExecuteCommand
                + ", " + m_quoteIvH.CancelAllLong + ", " + m_quoteIvH.CancelAllShort + ", " + m_quoteIvH.Qty
                + ", " + m_quoteIvH.ShiftIvPct + ", " + m_quoteIvH.StrikeStep + ") "
                + "[" + option.Symbol + "]",
                quoteIvChart,
                ListStyles.LINE, 12893590, LineStyles.SOLID, PaneSides.RIGHT,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, -16384, -8355712, 0, 0, DragableMode.None, Geometries.Rect, 12D, true,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, -1, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false,
                LabelHorizontalAlignment.Right, LabelVerticalAlignment.Center, -8355712, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false);
            smilePanePaneQuoteIvChart.AlternativeColor = 12893590;
            smilePanePaneQuoteIvChart.Thickness = 2;
            // Make 'ShowLongTargets' chart
            IGraphListBase smilePanePaneShowLongTargetsChart = smilePanePane.AddList("SmilePane_pane_ShowLongTargets_chart",
                "ShowLongTargets" + " (" + m_showLongTargetsH.IsLong + ") "
                + "[" + option.Symbol + "]",
                showLongTargetsChart,
                ListStyles.LINE, 45136, LineStyles.SOLID, PaneSides.RIGHT,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, -16732080, -8355712, 0, 0, DragableMode.None, Geometries.HorizontalDash, 12D, true,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, -1, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false,
                LabelHorizontalAlignment.Right, LabelVerticalAlignment.Center, -8355712, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false);
            smilePanePaneShowLongTargetsChart.AlternativeColor = 45136;
            smilePanePaneShowLongTargetsChart.Opacity = 255;
            // Make 'ShowShortTargets' chart
            IGraphListBase smilePanePaneShowShortTargetsChart = smilePanePane.AddList("SmilePane_pane_ShowShortTargets_chart",
                "ShowShortTargets" + " (" + m_showShortTargetsH.IsLong + ") "
                + "[" + option.Symbol + "]",
                showShortTargetsChart,
                ListStyles.LINE, 16711680, LineStyles.SOLID, PaneSides.RIGHT,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Top, -65536, -8355712, 0, 0, DragableMode.None, Geometries.HorizontalDash, 12D, true,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Top, -1, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false,
                LabelHorizontalAlignment.Right, LabelVerticalAlignment.Center, -8355712, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false);
            smilePanePaneShowShortTargetsChart.AlternativeColor = 16711680;
            smilePanePaneShowShortTargetsChart.Opacity = 255;
            // Make 'MktProfile' chart
            IGraphListBase posPanePaneMktProfileChart = posPanePane.AddList("PosPane_pane_MktProfile_chart",
                "MktProfile" + " (" + m_mktProfileH.GreekAlgo + ", " + m_mktProfileH.TooltipFormat
                + ", " + m_mktProfileH.NodesCount + ", " + m_mktProfileH.SigmaMult + ", " + m_mktProfileH.ShowNodes + ") "
                + "[" + option.Symbol + "]",
                mktProfileChart,
                ListStyles.LINE, MarketProfileLineColor /* 12603469 */, LineStyles.SOLID, PaneSides.RIGHT,
                new PointParameters(MarketProfileAnchorColor /* 16776960 */, DragableMode.None, Geometries.Ellipse, false),
                PointParameters.Empty,
                PointParameters.Empty);
            posPanePaneMktProfileChart.AlternativeColor = MarketProfileLineColor /* 12603469 */; // Темно-красный: 0xC0504D
            posPanePaneMktProfileChart.Thickness = 2;
            //posPanePane.UpdatePrecision(PaneSides.RIGHT, option.Decimals);
            // Make 'PortfolioProfile' chart
            IGraphListBase posPanePanePortfolioProfileChart = posPanePane.AddList("PosPane_pane_PortfolioProfile_chart",
                "PortfolioProfile" + " (" + m_portfolioProfileH.GreekAlgo + ", " + m_portfolioProfileH.TooltipFormat
                + ", " + m_portfolioProfileH.NodesCount + ", " + m_portfolioProfileH.SigmaMult + ", " + m_portfolioProfileH.ShowNodes + ") "
                + "[" + option.Symbol + "]",
                portfolioProfileChart,
                ListStyles.LINE, PortfolioProfileLineColor /* -16711936 */, LineStyles.SOLID, PaneSides.RIGHT,
                new PointParameters(PortfolioProfileAnchorColor /* 16776960 */, DragableMode.None, Geometries.Ellipse, false),
                PointParameters.Empty,
                PointParameters.Empty);
            posPanePaneMktProfileChart.AlternativeColor = PortfolioProfileLineColor /* -16711936 */; // Green: 0x00FF00
            posPanePaneMktProfileChart.Thickness = 2;
            // Make 'CurrentFutPx1' chart
            IGraphListBase posPanePaneCurrentFutPx1Chart = posPanePane.AddList("PosPane_pane_CurrentFutPx1_chart",
                "CurrentFutPx1" + " (" + m_currentFutPx1H.MinHeight
                + ", " + m_currentFutPx1H.OffsetPct + ", " + m_currentFutPx1H.Qty + ") "
                + "[" + option.Symbol + "]",
                currentFutPx1Chart,
                ListStyles.LINE, 16711680, LineStyles.SOLID, PaneSides.RIGHT,
                PointParameters.Empty,
                PointParameters.Empty,
                PointParameters.Empty);
            posPanePaneCurrentFutPx1Chart.AlternativeColor = 0;
            posPanePaneCurrentFutPx1Chart.Thickness = 2;
            // PROD-5746
            //posPanePane.UpdatePrecision(PaneSides.RIGHT, option.Decimals);
            // Make 'ExpiryProfile' chart
            IGraphListBase posPanePaneExpiryProfileChart = posPanePane.AddList("PosPane_pane_ExpiryProfile_chart",
                "ExpiryProfile" + " (" + m_expiryProfileH.GreekAlgo + ", " + m_expiryProfileH.TooltipFormat
                + ", " + m_expiryProfileH.NodesCount + ", " + m_expiryProfileH.SigmaMult + ", " + m_expiryProfileH.ShowNodes + ") "
                + "[" + option.Symbol + "]",
                expiryProfileChart,
                ListStyles.LINE, 14969866, LineStyles.SOLID, PaneSides.RIGHT,
                PointParameters.Empty,
                PointParameters.Empty,
                PointParameters.Empty);
            posPanePaneExpiryProfileChart.AlternativeColor = 0;
            // PROD-5746
            //posPanePane.UpdatePrecision(PaneSides.RIGHT, option.Decimals);
            // Make 'ZeroPnL' chart
            IGraphListBase posPanePaneZeroPnlChart = posPanePane.AddList("PosPane_pane_ZeroPnL_chart",
                "ZeroPnL" + " (" + m_zeroPnLh.SigmaPct + ", " + m_zeroPnLh.SigmaMult + ", " + m_zeroPnLh.ShowNodes + ") "
                + "[" + option.Symbol + "]",
                zeroPnlChart,
                ListStyles.LINE, 3245467, LineStyles.SOLID, PaneSides.RIGHT,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, 5210557, -8355712, 0, 0, DragableMode.None, Geometries.Cross, double.NaN, false,
                LabelHorizontalAlignment.Center, LabelVerticalAlignment.Bottom, 0, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false,
                LabelHorizontalAlignment.Right, LabelVerticalAlignment.Center, 0, -8355712, 0, 0, DragableMode.None, Geometries.Rect, double.NaN, false);
            posPanePaneZeroPnlChart.AlternativeColor = 3245467;
            posPanePaneZeroPnlChart.HideLastValue = true;
            // PROD-5746
            //posPanePane.UpdatePrecision(PaneSides.RIGHT, option.Decimals);
            // Make 'MktSimmProfile' chart
            IGraphListBase posPanePaneMktSimmProfileChart = posPanePane.AddList("PosPane_pane_MktSimmProfile_chart",
                "MktSimmProfile" + " (" + m_mktSimmProfileH.GreekAlgo + ", " + m_mktSimmProfileH.TooltipFormat
                + ", " + m_mktSimmProfileH.NodesCount + ", " + m_mktSimmProfileH.SigmaMult + ", " + m_mktSimmProfileH.ShowNodes + ") "
                + "[" + option.Symbol + "]",
                mktSimmProfileChart,
                ListStyles.LINE, ModelProfileLineColor /* 16777215 */, LineStyles.SOLID, PaneSides.RIGHT,
                new PointParameters(16711680, DragableMode.None, Geometries.SkewCross, false),
                PointParameters.Empty,
                PointParameters.Empty);
            posPanePaneMktSimmProfileChart.AlternativeColor = ModelProfileLineColor /* 16777215 */; // White: 0xFFFFFF
            posPanePaneMktSimmProfileChart.Thickness = 2;
            // PROD-5746
            //posPanePane.UpdatePrecision(PaneSides.RIGHT, option.Decimals);
            // =================================================
            // Controls
            // =================================================
            #region 'Visual settings' control pane
            {
                double top = UiControlFirstTop; // ANCHOR!
                // Make '759c1a68-dae7-4727-b07b-988311260fb7:SigmaMult' control
                IControl visualPanePaneManageSmilePaneSigmaMultControl = visualPanePane.AddControl(
                    "ManageSmilePane", RM.GetString("OptBoard.Label.WidthMultiplier"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, ManageSmilePaneSigmaMult);
                visualPanePaneManageSmilePaneSigmaMultControl.IsVisible = true;
                visualPanePaneManageSmilePaneSigmaMultControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 50

                // Make '759c1a68-dae7-4727-b07b-988311260fb7:ApplyVisualSettings' control
                IControl visualPanePaneManageSmilePaneApplyVisualSettingsControl = visualPanePane.AddControl(
                    "ManageSmilePane", RM.GetString("OptBoard.Label.Apply"), ControlParameterType.Button, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlBtnHeight, false, false, String.Empty, ManageSmilePaneApplyVisualSettings);
                visualPanePaneManageSmilePaneApplyVisualSettingsControl.IsVisible = true;
                visualPanePaneManageSmilePaneApplyVisualSettingsControl.IsNeedRecalculate = true;
                top += UiControlBtnHeight; // 80
            }
            #endregion 'Visual settings' control pane

            #region 'Market settings' control pane
            {
                double top = UiControlFirstTop; // ANCHOR!
                // Make '6d3626a2-ab61-418c-879f-2b64d6888a4c:DisplayPrice' control
                //IControl mktPanePaneBasePriceDisplayPriceControl = mktPanePane.AddControl(
                //    "BasePrice", RM.GetString("OptBoard.Label.FutPx"), ControlParameterType.NumericValue, true,
                //    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, BasePriceDisplayPrice);
                // Попробую настраивать точность цены динамически
                IOptimDataBase bpData = BasePriceDisplayPrice.Data;
                IControl mktPanePaneBasePriceDisplayPriceControl = mktPanePane.AddControl(
                    "BasePrice", RM.GetString("OptBoard.Label.FutPx"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty,
                    BasePriceDisplayPrice.Value, bpData.ItemId, bpData.InvariantName, bpData.Name, actualDecimals);
                mktPanePaneBasePriceDisplayPriceControl.IsVisible = true;
                mktPanePaneBasePriceDisplayPriceControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 50

                // Make '7dd5f841-f878-49cd-912f-5cc802076dce:Time' control
                IControl mktPanePaneDtTimeControl = mktPanePane.AddControl(
                    "dT", RM.GetString("OptBoard.Label.dT"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, DtTime);
                mktPanePaneDtTimeControl.IsVisible = true;
                mktPanePaneDtTimeControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 95

                top = 100; // ANCHOR! Думаю, хватит 5 пикселов отступ.
                // Make 'd6d6aca0-7bd1-4a24-a3b6-c9825b64a1f6:SetIvByHands' control
                IControl mktPanePaneGlobalSmileSetIvByHandsControl = mktPanePane.AddControl(
                    "GlobalSmile", RM.GetString("OptBoard.Label.SetIv") /* "Set IV" */, ControlParameterType.Checkbox, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlChkBoxHeight, false, false, String.Empty, GlobalSmileSetIvByHands);
                mktPanePaneGlobalSmileSetIvByHandsControl.IsVisible = true;
                mktPanePaneGlobalSmileSetIvByHandsControl.IsNeedRecalculate = true;
                top += UiControlChkBoxHeight; // 130

                // Make 'd6d6aca0-7bd1-4a24-a3b6-c9825b64a1f6:SetSlopeByHands' control
                IControl mktPanePaneGlobalSmileSetSlopeByHandsControl = mktPanePane.AddControl(
                    "GlobalSmile", RM.GetString("OptBoard.Label.SetSlope") /* "Set skew" */, ControlParameterType.Checkbox, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlChkBoxHeight, false, false, String.Empty, GlobalSmileSetSlopeByHands);
                mktPanePaneGlobalSmileSetSlopeByHandsControl.IsVisible = true;
                mktPanePaneGlobalSmileSetSlopeByHandsControl.IsNeedRecalculate = true;
                top += UiControlChkBoxHeight; // 160

                // Make 'd6d6aca0-7bd1-4a24-a3b6-c9825b64a1f6:IvAtmPct' control
                IControl mktPanePaneGlobalSmileIvAtmPctControl = mktPanePane.AddControl(
                    "GlobalSmile", RM.GetString("OptBoard.Label.IvAtmPct"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, GlobalSmileIvAtmPct);
                mktPanePaneGlobalSmileIvAtmPctControl.IsVisible = true;
                mktPanePaneGlobalSmileIvAtmPctControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 205

                // Make 'd6d6aca0-7bd1-4a24-a3b6-c9825b64a1f6:SlopePct' control
                IControl mktPanePaneGlobalSmileSlopePctControl = mktPanePane.AddControl(
                    "GlobalSmile", RM.GetString("OptBoard.Label.SlopePct") /* "Skew %" */, ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, GlobalSmileSlopePct);
                mktPanePaneGlobalSmileSlopePctControl.IsVisible = true;
                mktPanePaneGlobalSmileSlopePctControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 250

                // Make 'd6d6aca0-7bd1-4a24-a3b6-c9825b64a1f6:ShapePct' control
                IControl mktPanePaneGlobalSmileShapePctControl = mktPanePane.AddControl(
                    "GlobalSmile", RM.GetString("OptBoard.Label.ShapePct"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, GlobalSmileShapePct);
                mktPanePaneGlobalSmileShapePctControl.IsVisible = true;
                mktPanePaneGlobalSmileShapePctControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 295

                // Make '00cf523d-4906-4d30-a133-f403625775f2:SetIvByHands' control
                IControl mktPanePaneHedgeSmileSetIvByHandsControl = mktPanePane.AddControl(
                    "HedgeSmile", RM.GetString("OptBoard.Label.SetModelIv"), ControlParameterType.Checkbox, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlChkBoxHeight, false, false, String.Empty, HedgeSmileSetIvByHands);
                mktPanePaneHedgeSmileSetIvByHandsControl.IsVisible = true;
                mktPanePaneHedgeSmileSetIvByHandsControl.IsNeedRecalculate = true;
                top += UiControlChkBoxHeight; // 325

                // Make '00cf523d-4906-4d30-a133-f403625775f2:IvAtmPct' control
                IControl mktPanePaneHedgeSmileIvAtmPctControl = mktPanePane.AddControl(
                    "HedgeSmile", RM.GetString("OptBoard.Label.ModelIvPct"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, HedgeSmileIvAtmPct);
                mktPanePaneHedgeSmileIvAtmPctControl.IsVisible = true;
                mktPanePaneHedgeSmileIvAtmPctControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 370

                // [2016-10-21] PROD-4340 - Другой алгоритм построения модельной улыбки
                //// Make '5fac9f3b-ef65-44d4-8b76-337494efa496:SimmSmile' control
                //IControl mktPanePaneSimmSmileSimmWeightControl = mktPanePane.AddControl(
                //    "SimmSmile", RM.GetString("OptBoard.Label.SimmWeight"), ControlParameterType.NumericUpDown, true,
                //    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, SimmWeight);
                //mktPanePaneSimmSmileSimmWeightControl.IsVisible = true;
                //mktPanePaneSimmSmileSimmWeightControl.IsNeedRecalculate = true;
                //top += UiControlUpDownHeight; // 440

                // [2016-10-21] PROD-4340 - Другой алгоритм построения модельной улыбки
                // Make '00cf523d-4906-4d30-a133-f403625775f2:SlopePct' control
                IControl mktPanePaneHedgeSmileSlopePctControl = mktPanePane.AddControl(
                    "HedgeSmile", RM.GetString("OptBoard.Label.ModelSlopePct") /* "Model skew %" */, ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, HedgeSmileSlopePct);
                mktPanePaneHedgeSmileSlopePctControl.IsVisible = true;
                mktPanePaneHedgeSmileSlopePctControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 405

                // [2017-10-04] PROD-5352 - Портфельная улыбка
                // Make 'ed730713-ee93-4dcd-9f92-1e5d2566021d:SetIvByHands' control
                IControl mktPanePanePortfolioSmileSetIvByHandsControl = mktPanePane.AddControl(
                    "PortfolioSmile", RM.GetString("OptBoard.Label.SetPortfolioIv") /* "Set portfolio IV" */, ControlParameterType.Checkbox, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlChkBoxHeight, false, false, String.Empty, PortfolioSmileSetIvByHands);
                mktPanePanePortfolioSmileSetIvByHandsControl.IsVisible = true;
                mktPanePanePortfolioSmileSetIvByHandsControl.IsHelpVisible = false;
                mktPanePanePortfolioSmileSetIvByHandsControl.IsNeedRecalculate = true;
                top += UiControlChkBoxHeight; // 435

                // Make 'ed730713-ee93-4dcd-9f92-1e5d2566021d:SetSlopeByHands' control
                IControl mktPanePanePortfolioSmileSetSlopeByHandsControl = mktPanePane.AddControl(
                    "PortfolioSmile", RM.GetString("OptBoard.Label.SetPortfolioSkew") /* "Set portfolio skew" */, ControlParameterType.Checkbox, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlChkBoxHeight, false, false, String.Empty, PortfolioSmileSetSlopeByHands);
                mktPanePanePortfolioSmileSetSlopeByHandsControl.IsVisible = true;
                mktPanePanePortfolioSmileSetSlopeByHandsControl.IsHelpVisible = false;
                mktPanePanePortfolioSmileSetSlopeByHandsControl.IsNeedRecalculate = true;
                top += UiControlChkBoxHeight; // 465

                // Make 'ed730713-ee93-4dcd-9f92-1e5d2566021d:IvAtmPct' control
                IControl mktPanePanePortfolioSmileIvAtmPctControl = mktPanePane.AddControl(
                    "PortfolioSmile", RM.GetString("OptBoard.Label.PortfolioIvPct") /* "Portfolio IV %" */, ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, PortfolioSmileIvAtmPct);
                mktPanePanePortfolioSmileIvAtmPctControl.IsVisible = true;
                mktPanePanePortfolioSmileIvAtmPctControl.IsHelpVisible = false;
                mktPanePanePortfolioSmileIvAtmPctControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 510

                // Make 'ed730713-ee93-4dcd-9f92-1e5d2566021d:SlopePct' control
                IControl mktPanePanePortfolioSmileSlopePctControl = mktPanePane.AddControl(
                    "PortfolioSmile", RM.GetString("OptBoard.Label.PortfolioSlopePct") /* "Portfolio skew %" */, ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, PortfolioSmileSlopePct);
                mktPanePanePortfolioSmileSlopePctControl.IsVisible = true;
                mktPanePanePortfolioSmileSlopePctControl.IsHelpVisible = false;
                mktPanePanePortfolioSmileSlopePctControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 555

                // Make 'ed730713-ee93-4dcd-9f92-1e5d2566021d:ShapePct' control
                IControl mktPanePanePortfolioSmileShapePctControl = mktPanePane.AddControl(
                    "PortfolioSmile", RM.GetString("OptBoard.Label.PortfolioShapePct") /* "Portfolio shape %" */, ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, PortfolioSmileShapePct);
                mktPanePanePortfolioSmileShapePctControl.IsVisible = true;
                mktPanePanePortfolioSmileShapePctControl.IsHelpVisible = false;
                mktPanePanePortfolioSmileShapePctControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 600
            }
            #endregion 'Market settings' control pane

            #region 'Hedge settings' control pane
            {
                double top = UiControlFirstTop; // ANCHOR!
                // Make '82f1342b-32e4-4434-98bb-3aedeceb601c:HedgeDelta' control
                IControl hedgeControlPanePaneAutoHedgeHedgeDeltaControl = hedgeControlPanePane.AddControl(
                    "AutoHedge", RM.GetString("OptBoard.Label.Autohedge"), ControlParameterType.Checkbox, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlChkBoxHeight, false, false, String.Empty, AutoHedgeHedgeDelta);
                hedgeControlPanePaneAutoHedgeHedgeDeltaControl.IsVisible = true;
                hedgeControlPanePaneAutoHedgeHedgeDeltaControl.IsNeedRecalculate = true;
                top += UiControlChkBoxHeight; // 35

                // Make '82f1342b-32e4-4434-98bb-3aedeceb601c:UpDelta' control
                IControl hedgeControlPanePaneAutoHedgeUpDeltaControl = hedgeControlPanePane.AddControl(
                    "AutoHedge", RM.GetString("OptBoard.Label.UpDelta"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, AutoHedgeUpDelta);
                hedgeControlPanePaneAutoHedgeUpDeltaControl.IsVisible = true;
                hedgeControlPanePaneAutoHedgeUpDeltaControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 80

                // Make '82f1342b-32e4-4434-98bb-3aedeceb601c:TargetDelta' control
                IControl hedgeControlPanePaneAutoHedgeTargetDeltaControl = hedgeControlPanePane.AddControl(
                    "AutoHedge", RM.GetString("OptBoard.Label.TargetDelta"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, AutoHedgeTargetDelta);
                hedgeControlPanePaneAutoHedgeTargetDeltaControl.IsVisible = true;
                hedgeControlPanePaneAutoHedgeTargetDeltaControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 125

                // Make '82f1342b-32e4-4434-98bb-3aedeceb601c:DownDelta' control
                IControl hedgeControlPanePaneAutoHedgeDownDeltaControl = hedgeControlPanePane.AddControl(
                    "AutoHedge", RM.GetString("OptBoard.Label.DownDelta"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, AutoHedgeDownDelta);
                hedgeControlPanePaneAutoHedgeDownDeltaControl.IsVisible = true;
                hedgeControlPanePaneAutoHedgeDownDeltaControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 170

                // Make '82f1342b-32e4-4434-98bb-3aedeceb601c:MinPeriod' control
                IControl hedgeControlPanePaneAutoHedgeMinPeriodControl = hedgeControlPanePane.AddControl(
                    "AutoHedge", RM.GetString("OptBoard.Label.Period"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlHalfWidth, UiControlUpDownHeight, false, false, String.Empty, AutoHedgeMinPeriod);
                hedgeControlPanePaneAutoHedgeMinPeriodControl.IsVisible = false;
                hedgeControlPanePaneAutoHedgeMinPeriodControl.IsNeedRecalculate = true;
                top += 0; // 170

                // Make '82f1342b-32e4-4434-98bb-3aedeceb601c:SensitivityPct' control
                IControl hedgeControlPanePaneAutoHedgeSensitivityPctControl = hedgeControlPanePane.AddControl(
                    "AutoHedge", RM.GetString("OptBoard.Label.SensitivityPct"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft + 0 /* UiControlHalfWidth */, top, UiControlFullWidth /* UiControlHalfWidth */, UiControlUpDownHeight, false, false, String.Empty, AutoHedgeSensitivityPct);
                hedgeControlPanePaneAutoHedgeSensitivityPctControl.IsVisible = true;
                hedgeControlPanePaneAutoHedgeSensitivityPctControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 215

                // Make '82f1342b-32e4-4434-98bb-3aedeceb601c:BuyShift' control
                IControl hedgeControlPanePaneAutoHedgeBuyShiftControl = hedgeControlPanePane.AddControl(
                    "AutoHedge", RM.GetString("OptBoard.Label.BuyShift"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth /* UiControlHalfWidth */, UiControlUpDownHeight, false, false, String.Empty, AutoHedgeBuyShift);
                hedgeControlPanePaneAutoHedgeBuyShiftControl.IsVisible = true;
                hedgeControlPanePaneAutoHedgeBuyShiftControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight /* 0 */; // 260

                // Make '82f1342b-32e4-4434-98bb-3aedeceb601c:SellShift' control
                IControl hedgeControlPanePaneAutoHedgeSellShiftControl = hedgeControlPanePane.AddControl(
                    "AutoHedge", RM.GetString("OptBoard.Label.SellShift"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft + 0 /* UiControlHalfWidth */, top, UiControlFullWidth /* UiControlHalfWidth */, UiControlUpDownHeight, false, false, String.Empty, AutoHedgeSellShift);
                hedgeControlPanePaneAutoHedgeSellShiftControl.IsVisible = true;
                hedgeControlPanePaneAutoHedgeSellShiftControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 305

                // Make '82f1342b-32e4-4434-98bb-3aedeceb6f1f:BuyPrice' control
                IControl hedgeControlPanePaneAutoHedgeBuyPriceControl = hedgeControlPanePane.AddControl(
                    "AutoHedge", RM.GetString("OptBoard.Label.BuyPrice"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, true, false, String.Empty, AutoHedgeBuyPrice);
                hedgeControlPanePaneAutoHedgeBuyPriceControl.IsVisible = true;
                hedgeControlPanePaneAutoHedgeBuyPriceControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 350

                // Make '82f1342b-32e4-4434-98cc-3aedeceb6e1e:SellPrice' control
                IControl hedgeControlPanePaneAutoHedgeSellPriceControl = hedgeControlPanePane.AddControl(
                    "AutoHedge", RM.GetString("OptBoard.Label.SellPrice"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, true, false, String.Empty, AutoHedgeSellPrice);
                hedgeControlPanePaneAutoHedgeSellPriceControl.IsVisible = true;
                hedgeControlPanePaneAutoHedgeSellPriceControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 395
            }
            #endregion 'Hedge settings' control pane

            #region 'Trade settings' control pane
            {
                double top = UiControlFirstTop; // ANCHOR!
                // Make 'e2d672d8-f0e5-4001-a99b-6709ffd9f21c:Profit' control
                //IControl smileControlPanePaneTotalProfitProfitControl = smileControlPanePane.AddControl(
                //    "TotalProfit", RM.GetString("OptBoard.Label.Profit"), ControlParameterType.NumericValue, true,
                //    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, TotalProfitDisplayValue);
                // Попробую настраивать точность цены динамически
                IOptimDataBase pnlData = TotalProfitDisplayValue.Data;
                IControl smileControlPanePaneTotalProfitProfitControl = smileControlPanePane.AddControl(
                    "TotalProfit", RM.GetString("OptBoard.Label.Profit"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty,
                    TotalProfitDisplayValue.Value, pnlData.ItemId, pnlData.InvariantName, pnlData.Name, actualDecimals + 1); // Точность профита делаю чуть-чуть выше точности цены фьючерса
                smileControlPanePaneTotalProfitProfitControl.IsVisible = true;
                smileControlPanePaneTotalProfitProfitControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 50

                // Make '46c9b139-ba2f-4095-8590-57f4dbe705df:Delta' control
                IControl smileControlPanePaneNdaiSimmDeltaControl = smileControlPanePane.AddControl(
                    "ndaiSimm", RM.GetString("OptBoard.Label.DeltaFromModel"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, NdaiSimmDelta);
                smileControlPanePaneNdaiSimmDeltaControl.IsVisible = true;
                smileControlPanePaneNdaiSimmDeltaControl.IsNeedRecalculate = false;
                top += UiControlUpDownHeight; // 95

                // Make 'b77c8226-c66b-4caf-8ef2-115fd600b047:Delta' control
                IControl smileControlPanePaneNumDeltaAtmIntDeltaControl = smileControlPanePane.AddControl(
                    "NumDeltaATMInt", RM.GetString("OptBoard.Label.DeltaFromMarket"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, NumDeltaAtmIntDelta);
                smileControlPanePaneNumDeltaAtmIntDeltaControl.IsVisible = true;
                smileControlPanePaneNumDeltaAtmIntDeltaControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 140

                // Make '5a6de5b6-7047-42dc-af6d-4ca00ed75ffa:Gamma' control
                IControl smileControlPanePaneNumGamAtmIntGammaControl = smileControlPanePane.AddControl(
                    "NumGamATMInt", RM.GetString("OptBoard.Label.Gamma"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, NumGamAtmIntGamma);
                smileControlPanePaneNumGamAtmIntGammaControl.IsVisible = true;
                smileControlPanePaneNumGamAtmIntGammaControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 185

                // Make 'bd0476cb-0b20-47c3-aedd-0af02efd36bd:Theta' control
                IControl smileControlPanePaneNumerThetaAtmThetaControl = smileControlPanePane.AddControl(
                    "NumerThetaATM", RM.GetString("OptBoard.Label.Theta"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlHalfWidth, UiControlNumHeight, false, false, String.Empty, NumerThetaAtmTheta);
                smileControlPanePaneNumerThetaAtmThetaControl.IsVisible = true;
                smileControlPanePaneNumerThetaAtmThetaControl.IsNeedRecalculate = false;
                top += 0; // 185

                // Make '142a4b0b-3b89-4cb3-9801-07897c970190:Vega' control
                IControl smileControlPanePaneNumerVegaAtmVegaControl = smileControlPanePane.AddControl(
                    "NumerVegaATM", RM.GetString("OptBoard.Label.Vega"), ControlParameterType.NumericValue, true,
                    UiControlLeft + UiControlHalfWidth, top, UiControlHalfWidth, UiControlNumHeight, false, false, String.Empty, NumerVegaAtmVega);
                smileControlPanePaneNumerVegaAtmVegaControl.IsVisible = true;
                smileControlPanePaneNumerVegaAtmVegaControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 230

                // PROD-5353 - Заменяем два контрола выбора типа опциона на один
                // Make '92cd5616-549f-4ff3-9a4c-6201f85f349c:OptionType' control
                IControl smileControlPanePaneIvAsksOptionTypeControl = smileControlPanePane.AddControl(
                    "IvAsks", RM.GetString("OptBoard.Label.OptionType"), ControlParameterType.EnumComboBox, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlDropHeight, false, false, String.Empty, IvOptionType /* IvAsksOptionType */);
                smileControlPanePaneIvAsksOptionTypeControl.IsVisible = true;
                smileControlPanePaneIvAsksOptionTypeControl.IsNeedRecalculate = true;
                top += UiControlDropHeight; // 275

                // PROD-5353 - Заменяем два контрола выбора типа опциона на один
                //// Make '948ffcfb-08b7-411f-a240-4aae9baa7d87:OptionType' control
                //IControl smileControlPanePaneIvBidsOptionTypeControl = smileControlPanePane.AddControl(
                //    "IvBids", RM.GetString("OptBoard.Label.OptionTypeBid"), ControlParameterType.EnumComboBox, true,
                //    UiControlLeft, top, UiControlFullWidth, UiControlDropHeight, false, false, String.Empty, IvBidsOptionType);
                //top += UiControlDropHeight; // 320

                // Make '2bda62da-a3ea-417d-b333-929e7757a09f:Qty' control
                IControl smileControlPanePaneCurrentFutPxQtyControl = smileControlPanePane.AddControl(
                    "CurrentFutPx", RM.GetString("OptBoard.Label.OrderQtyFut"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, CurrentFutPxQty);
                smileControlPanePaneCurrentFutPxQtyControl.IsVisible = true;
                smileControlPanePaneCurrentFutPxQtyControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 320

                // Make '1ea8f723-fd0d-4b97-a94c-28f7b48b9e40:Qty' control
                IControl smileControlPanePaneTradeAsksQtyControl = smileControlPanePane.AddControl(
                    "TradeAsks", RM.GetString("OptBoard.Label.OrderQtyOptAsk"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, TradeAsksQty);
                smileControlPanePaneTradeAsksQtyControl.IsVisible = true;
                smileControlPanePaneTradeAsksQtyControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 365

                // Make '6779e7e6-1a7b-456e-bad8-e01f592fa939:Qty' control
                IControl smileControlPanePaneTradeBidsQtyControl = smileControlPanePane.AddControl(
                    "TradeBids", RM.GetString("OptBoard.Label.OrderQtyOptBid"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, TradeBidsQty);
                smileControlPanePaneTradeBidsQtyControl.IsVisible = true;
                smileControlPanePaneTradeBidsQtyControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 410

                // Make '1ea8f723-fd0d-4b97-a94c-28f7b48b9e40:WidthPx' control
                IControl smileControlPanePaneTradeAsksWidthPxControl = smileControlPanePane.AddControl(
                    "TradeAsks", RM.GetString("OptBoard.Label.DepthOptAsk"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, TradeAsksWidthPx);
                smileControlPanePaneTradeAsksWidthPxControl.IsVisible = true;
                smileControlPanePaneTradeAsksWidthPxControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 455

                // Make '6779e7e6-1a7b-456e-bad8-e01f592fa939:WidthPx' control
                IControl smileControlPanePaneTradeBidsWidthPxControl = smileControlPanePane.AddControl(
                    "TradeBids", RM.GetString("OptBoard.Label.DepthOptBid"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, TradeBidsWidthPx);
                smileControlPanePaneTradeBidsWidthPxControl.IsVisible = true;
                smileControlPanePaneTradeBidsWidthPxControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 500

                //// Make '74530095-d19f-489b-996e-70f55e76a79f:BlockTrading' control
                //IControl smileControlPanePanePosManBlockTradingControl = smileControlPanePane.AddControl(
                //    "PosMan", "Block trading", ControlParameterType.Checkbox, true,
                //    UiControlLeft, 10D, UiControlFullWidth, UiControlChkBoxHeight, true, false, String.Empty, PosManBlockTrading);
                //smileControlPanePanePosManBlockTradingControl.IsVisible = false;
                //smileControlPanePanePosManBlockTradingControl.IsNeedRecalculate = true;
                //// Make '74530095-d19f-489b-996e-70f55e76a79f:UseVirtualPositions' control
                //IControl smileControlPanePanePosManUseVirtualPositionsControl =
                //    smileControlPanePane.AddControl("PosMan", "Use virtual positions", ControlParameterType.Checkbox, true,
                //    UiControlLeft, 40D, UiControlFullWidth, UiControlChkBoxHeight, true, false, String.Empty, PosManUseVirtualPositions);
                //smileControlPanePanePosManUseVirtualPositionsControl.IsVisible = false;
                //smileControlPanePanePosManUseVirtualPositionsControl.IsNeedRecalculate = true;
                //// Make '74530095-d19f-489b-996e-70f55e76a79f:DropVirtualPos' control
                //IControl smileControlPanePanePosManDropVirtualPosControl =
                //    smileControlPanePane.AddControl("PosMan", "Drop virt pos", ControlParameterType.Button, true,
                //    UiControlLeft + UiControlHalfWidth, 120D, UiControlHalfWidth, UiControlBtnHeight, true, false, String.Empty, PosManDropVirtualPos);
                //smileControlPanePanePosManDropVirtualPosControl.IsVisible = false;
                //smileControlPanePanePosManDropVirtualPosControl.IsNeedRecalculate = true;
            }
            #endregion 'Trade settings' control pane

            #region 'Position overview' control pane
            {
                double top = UiControlFirstTop; // ANCHOR!

                // Make 'e2d672d8-f0e5-4001-a99b-6709ffd9f21c:Profit' control
                //IControl posControlPanePaneTotalProfitProfitControl = posControlPanePane.AddControl(
                //    "TotalProfit", RM.GetString("OptBoard.Label.Profit"), ControlParameterType.NumericValue, true,
                //    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, TotalProfitDisplayValue);
                // Попробую настраивать точность цены динамически
                IOptimDataBase pnlData = TotalProfitDisplayValue.Data;
                IControl posControlPanePaneTotalProfitProfitControl = posControlPanePane.AddControl(
                    "TotalProfit", RM.GetString("OptBoard.Label.Profit"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty,
                    TotalProfitDisplayValue.Value, pnlData.ItemId, pnlData.InvariantName, pnlData.Name, actualDecimals + 1); // Точность профита делаю чуть-чуть выше точности цены фьючерса
                posControlPanePaneTotalProfitProfitControl.IsVisible = true;
                posControlPanePaneTotalProfitProfitControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 50

                // Make '46c9b139-ba2f-4095-8590-57f4dbe705df:Delta' control
                IControl posControlPanePaneNdaiSimmDeltaControl = posControlPanePane.AddControl(
                    "ndaiSimm", RM.GetString("OptBoard.Label.DeltaFromModel"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, NdaiSimmDelta);
                posControlPanePaneNdaiSimmDeltaControl.IsVisible = true;
                posControlPanePaneNdaiSimmDeltaControl.IsNeedRecalculate = false;
                top += UiControlUpDownHeight; // 95

                // Make 'b77c8226-c66b-4caf-8ef2-115fd600b047:Delta' control
                IControl posControlPanePaneNumDeltaAtmIntDeltaControl = posControlPanePane.AddControl(
                    "NumDeltaATMInt", RM.GetString("OptBoard.Label.DeltaFromMarket"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, NumDeltaAtmIntDelta);
                posControlPanePaneNumDeltaAtmIntDeltaControl.IsVisible = true;
                posControlPanePaneNumDeltaAtmIntDeltaControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 140

                // Make '5a6de5b6-7047-42dc-af6d-4ca00ed75ffa:Gamma' control
                IControl posControlPanePaneNumGamAtmIntGammaControl = posControlPanePane.AddControl(
                    "NumGamATMInt", RM.GetString("OptBoard.Label.Gamma"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, NumGamAtmIntGamma);
                posControlPanePaneNumGamAtmIntGammaControl.IsVisible = true;
                posControlPanePaneNumGamAtmIntGammaControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 185

                // Make 'bd0476cb-0b20-47c3-aedd-0af02efd36bd:Theta' control
                IControl posControlPanePaneNumerThetaAtmThetaControl = posControlPanePane.AddControl(
                    "NumerThetaATM", RM.GetString("OptBoard.Label.Theta"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlHalfWidth, UiControlNumHeight, false, false, String.Empty, NumerThetaAtmTheta);
                posControlPanePaneNumerThetaAtmThetaControl.IsVisible = true;
                posControlPanePaneNumerThetaAtmThetaControl.IsNeedRecalculate = false;
                top += 0; // 185

                // Make '142a4b0b-3b89-4cb3-9801-07897c970190:Vega' control
                IControl posControlPanePaneNumerVegaAtmVegaControl = posControlPanePane.AddControl(
                    "NumerVegaATM", RM.GetString("OptBoard.Label.Vega"), ControlParameterType.NumericValue, true,
                    UiControlLeft + UiControlHalfWidth, top, UiControlHalfWidth, UiControlNumHeight, false, false, String.Empty, NumerVegaAtmVega);
                posControlPanePaneNumerVegaAtmVegaControl.IsVisible = true;
                posControlPanePaneNumerVegaAtmVegaControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 230

                // Make '6d3626a2-ab61-418c-879f-2b64d6888a4c:DisplayPrice' control
                //IControl posControlPanePaneBasePriceDisplayPriceControl = posControlPanePane.AddControl(
                //    "BasePrice", RM.GetString("OptBoard.Label.FutPx"), ControlParameterType.NumericValue, true,
                //    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, BasePriceDisplayPrice);
                // Попробую настраивать точность цены динамически
                IOptimDataBase bpData = BasePriceDisplayPrice.Data;
                IControl posControlPanePaneBasePriceDisplayPriceControl = posControlPanePane.AddControl(
                    "BasePrice", RM.GetString("OptBoard.Label.FutPx"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty,
                    BasePriceDisplayPrice.Value, bpData.ItemId, bpData.InvariantName, bpData.Name, actualDecimals);
                posControlPanePaneBasePriceDisplayPriceControl.IsVisible = true;
                posControlPanePaneBasePriceDisplayPriceControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 275

                // Make '7dd5f841-f878-49cd-912f-5cc802076dce:Time' control
                IControl posControlPanePaneDtTimeControl = posControlPanePane.AddControl(
                    "dT", RM.GetString("OptBoard.Label.dT"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, DtTime);
                posControlPanePaneDtTimeControl.IsVisible = true;
                posControlPanePaneDtTimeControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 320

                // Make '1156553f-7842-4858-9799-839ab55d4b2b:OpenQty' control
                IControl posControlPanePaneTotalQtyOpenQtyControl = posControlPanePane.AddControl(
                    "TotalQty", RM.GetString("OptBoard.Label.TotalFutQty"), ControlParameterType.NumericValue, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlNumHeight, false, false, String.Empty, TotalQtyOpenQty);
                posControlPanePaneTotalQtyOpenQtyControl.IsVisible = true;
                posControlPanePaneTotalQtyOpenQtyControl.IsNeedRecalculate = false;
                top += UiControlNumHeight; // 365

                //// Make '74530095-d19f-489b-996e-70f55e76a79f:DropVirtualPos' control
                // Этот контрол сейчас не отображается насколько понимаю
                //IControl posControlPanePanePosManDropVirtualPosControl = posControlPanePane.AddControl(
                //    "PosMan", "Drop virt pos", ControlParameterType.Button, true,
                //    UiControlLeft + UiControlHalfWidth, 190D, UiControlHalfWidth, UiControlBtnHeight, true, false, String.Empty, PosManDropVirtualPos);
                //posControlPanePanePosManDropVirtualPosControl.IsVisible = false;
                //posControlPanePanePosManDropVirtualPosControl.IsNeedRecalculate = true;
            }
            #endregion 'Position overview' control pane

            #region 'Quote settings' control pane
            {
                double top = UiControlFirstTop; // ANCHOR!
                // Make '7acddef7-d268-426b-8f4a-14e687a961f8:ShiftIvPct' control
                IControl quotePanePaneQuoteIvShiftIvPctControl = quotePanePane.AddControl(
                    "QuoteIv", RM.GetString("OptBoard.Label.ShiftSmilePct"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, QuoteIvShiftIvPct);
                quotePanePaneQuoteIvShiftIvPctControl.IsVisible = true;
                quotePanePaneQuoteIvShiftIvPctControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 50

                // Make '7acddef7-d268-426b-8f4a-14e687a961f8:ShiftIvPct' control
                IControl quotePanePaneQuoteIvShiftPriceControl = quotePanePane.AddControl(
                    "QuoteIv", RM.GetString("OptBoard.Label.ShiftSmilePrice"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlUpDownHeight, false, false, String.Empty, QuoteIvShiftPrice);
                quotePanePaneQuoteIvShiftPriceControl.IsVisible = true;
                quotePanePaneQuoteIvShiftPriceControl.IsNeedRecalculate = true;
                top += UiControlUpDownHeight; // 95

                // Make 'aaaddef7-d268-426b-8f4a-14e687a961f8:Strike' control
                IControl quotePanePaneQuoteIvStrikeControl = quotePanePane.AddControl(
                    "QuoteIv", RM.GetString("OptBoard.Label.Strike"), ControlParameterType.StringComboBox, true,
                    UiControlLeft, top, UiControlHalfWidth, UiControlDropHeight, false, false, String.Empty, QuoteIvStrike, m_quoteIvH.GetValuesForParameter("Strike"));
                quotePanePaneQuoteIvStrikeControl.IsVisible = true;
                quotePanePaneQuoteIvStrikeControl.IsNeedRecalculate = true;
                top += 0; // 95

                // Make 'fffd5616-549f-4ff3-9a4c-6201f85f3aaa:OptionType' control
                IControl quotePanePaneQuoteIvOptionTypeControl = quotePanePane.AddControl(
                    "QuoteIv", RM.GetString("OptBoard.Label.OptionType"), ControlParameterType.EnumComboBox, true,
                    UiControlLeft + UiControlHalfWidth, top, UiControlHalfWidth, UiControlDropHeight, false, false, String.Empty, QuoteIvOptionType);
                quotePanePaneQuoteIvOptionTypeControl.IsVisible = true;
                quotePanePaneQuoteIvOptionTypeControl.IsNeedRecalculate = true;
                top += UiControlDropHeight; // 140

                // Make '7acddef7-d268-426b-8f4a-14e687a961f8:Qty' control
                IControl quotePanePaneQuoteIvQtyControl = quotePanePane.AddControl(
                    "QuoteIv", RM.GetString("OptBoard.Label.OrderQtyIvTarget"), ControlParameterType.NumericUpDown, true,
                    UiControlLeft, top, UiControlHalfWidth - 10, UiControlUpDownHeight, false, false, String.Empty, QuoteIvQty);
                quotePanePaneQuoteIvQtyControl.IsVisible = true;
                quotePanePaneQuoteIvQtyControl.IsNeedRecalculate = true;
                top += 14; // 154 -- небольшой сдвиг кнопки, чтобы лучше смотрелось рядом с NumericUpDown

                // Make '7acddef7-d268-426b-8f4a-14e687a961f8:ExecuteCommand' control
                IControl quotePanePaneQuoteIvExecuteCommandControl = quotePanePane.AddControl(
                    "QuoteIv", RM.GetString("OptBoard.Label.Execute"), ControlParameterType.Button, true,
                    UiControlLeft + UiControlHalfWidth + 10, top, UiControlHalfWidth - 10, UiControlBtnHeight, false, false, String.Empty, QuoteIvExecuteCommand);
                quotePanePaneQuoteIvExecuteCommandControl.IsVisible = true;
                quotePanePaneQuoteIvExecuteCommandControl.IsNeedRecalculate = true;
                top += UiControlBtnHeight; // 184

                top = 190; // ANCHOR! Кнопка как-то неправильно свою высоту рассчитывает. Поэтому нужен новый якорь? Достаточно 6 пикселов отступить.
                // Make '7acddef7-d268-426b-8f4a-14e687a961f8:CancelAllLong' control
                IControl quotePanePaneQuoteIvCancelAllLongControl = quotePanePane.AddControl(
                    "QuoteIv", RM.GetString("OptBoard.Label.CancelAllLong"), ControlParameterType.Button, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlBtnHeight, false, false, String.Empty, QuoteIvCancelAllLong);
                quotePanePaneQuoteIvCancelAllLongControl.IsVisible = true;
                quotePanePaneQuoteIvCancelAllLongControl.IsNeedRecalculate = true;
                top += UiControlBtnHeight; // 220

                // Make '7acddef7-d268-426b-8f4a-14e687a961f8:CancelAllShort' control
                IControl quotePanePaneQuoteIvCancelAllShortControl = quotePanePane.AddControl(
                    "QuoteIv", RM.GetString("OptBoard.Label.CancelAllShort"), ControlParameterType.Button, true,
                    UiControlLeft, top, UiControlFullWidth, UiControlBtnHeight, false, false, String.Empty, QuoteIvCancelAllShort);
                quotePanePaneQuoteIvCancelAllShortControl.IsVisible = true;
                quotePanePaneQuoteIvCancelAllShortControl.IsNeedRecalculate = true;
                top += UiControlBtnHeight; // 250
            }
            #endregion 'Quote settings' control pane

            #region Qty data grid
            {
                int col = 3;
                // Make 'QtyCalls' dataGrid
                dataGridPaneDataGridPane.AddList("DataGridPane_dataGridPane_QtyCalls_dataGrid",
                    "QtyCalls" + " (" + m_qtyCallsH.OptionType + ", " + m_qtyCallsH.CountFutures
                    + ", " + m_qtyCallsH.TooltipFormat + ", " + m_qtyCallsH.SigmaMult
                    + ", " + m_qtyCallsH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    qtyCallsChart, col++, qtyFormat, "Qty Call", true, TextAlignment.Left, null);
                // Make 'QtyPuts' dataGrid
                dataGridPaneDataGridPane.AddList("DataGridPane_dataGridPane_QtyPuts_dataGrid",
                    "QtyPuts" + " (" + m_qtyPutsH.OptionType + ", " + m_qtyPutsH.CountFutures
                    + ", " + m_qtyPutsH.TooltipFormat + ", " + m_qtyPutsH.SigmaMult
                    + ", " + m_qtyPutsH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    qtyPutsChart, col++, qtyFormat, "Qty Put", true, TextAlignment.Left, null);
                // Make 'QtyTotal' dataGrid
                dataGridPaneDataGridPane.AddList("DataGridPane_dataGridPane_QtyTotal_dataGrid",
                    "QtyTotal" + " (" + m_qtyTotalH.OptionType + ", " + m_qtyTotalH.CountFutures
                    + ", " + m_qtyTotalH.TooltipFormat + ", " + m_qtyTotalH.SigmaMult
                    + ", " + m_qtyTotalH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    qtyTotalChart, col++, qtyFormat, "Total", true, TextAlignment.Left, null);
            }
            #endregion Qty data grid

            #region Average price data grid
            {
                int col = 3;
                // Make 'LongPutPxs' dataGrid
                priceGridDataGridPane.AddList("PriceGrid_dataGridPane_LongPutPxs_dataGrid",
                    "LongPutPxs" + " (" + m_longPutPxsH.LongPositions + ", " + m_longPutPxsH.OptionType
                    + ", " + m_longPutPxsH.CountFutures + ", " + m_longPutPxsH.TooltipFormat
                    + ", " + m_longPutPxsH.SigmaMult + ", " + m_longPutPxsH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    longPutPxsChart, col++, AveragePriceFormat, "LongPuts", true, TextAlignment.Left, null);
                // Make 'LongPutQty' dataGrid
                priceGridDataGridPane.AddList("PriceGrid_dataGridPane_LongPutQty_dataGrid",
                    "LongPutQty" + " (" + m_longPutQtyH.LongPositions + ", " + m_longPutQtyH.CountQty
                    + ", " + m_longPutQtyH.OptionType + ", " + m_longPutQtyH.CountFutures + ", " + m_longPutQtyH.TooltipFormat
                    + ", " + m_longPutQtyH.SigmaMult + ", " + m_longPutQtyH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    longPutQtyChart, col++, qtyFormat, "LPQty", true, TextAlignment.Left, null);
                // Make 'ShortPutPxs' dataGrid
                priceGridDataGridPane.AddList("PriceGrid_dataGridPane_ShortPutPxs_dataGrid",
                    "ShortPutPxs" + " (" + m_shortPutPxsH.LongPositions + ", " + m_shortPutPxsH.OptionType
                    + ", " + m_shortPutPxsH.CountFutures + ", " + m_shortPutPxsH.TooltipFormat
                    + ", " + m_shortPutPxsH.SigmaMult + ", " + m_shortPutPxsH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    shortPutPxsChart, col++, AveragePriceFormat, "ShortPuts", true, TextAlignment.Left, null);
                // Make 'ShortPutQty' dataGrid
                priceGridDataGridPane.AddList("PriceGrid_dataGridPane_ShortPutQty_dataGrid",
                    "ShortPutQty" + " (" + m_shortPutQtyH.LongPositions + ", " + m_shortPutQtyH.CountQty
                    + ", " + m_shortPutQtyH.OptionType + ", " + m_shortPutQtyH.CountFutures + ", " + m_shortPutQtyH.TooltipFormat
                    + ", " + m_shortPutQtyH.SigmaMult + ", " + m_shortPutQtyH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    shortPutQtyChart, col++, qtyFormat, "SPQty", true, TextAlignment.Left, null);
                // Make 'LongCallPxs' dataGrid
                priceGridDataGridPane.AddList("PriceGrid_dataGridPane_LongCallPxs_dataGrid",
                    "LongCallPxs" + " (" + m_longCallPxsH.LongPositions + ", " + m_longCallPxsH.OptionType
                    + ", " + m_longCallPxsH.CountFutures + ", " + m_longCallPxsH.TooltipFormat
                    + ", " + m_longCallPxsH.SigmaMult + ", " + m_longCallPxsH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    longCallPxsChart, col++, AveragePriceFormat, "LongCalls", true, TextAlignment.Left, null);
                // Make 'LongCallQty' dataGrid
                priceGridDataGridPane.AddList("PriceGrid_dataGridPane_LongCallQty_dataGrid",
                    "LongCallQty" + " (" + m_longCallQtyH.LongPositions + ", " + m_longCallQtyH.CountQty
                    + ", " + m_longCallQtyH.OptionType + ", " + m_longCallQtyH.CountFutures + ", " + m_longCallQtyH.TooltipFormat
                    + ", " + m_longCallQtyH.SigmaMult + ", " + m_longCallQtyH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    longCallQtyChart, col++, qtyFormat, "LCQty", true, TextAlignment.Left, null);
                // Make 'ShortCallPxs' dataGrid
                priceGridDataGridPane.AddList("PriceGrid_dataGridPane_ShortCallPxs_dataGrid",
                    "ShortCallPxs" + " (" + m_shortCallPxsH.LongPositions + ", " + m_shortCallPxsH.OptionType
                    + ", " + m_shortCallPxsH.CountFutures + ", " + m_shortCallPxsH.TooltipFormat
                    + ", " + m_shortCallPxsH.SigmaMult + ", " + m_shortCallPxsH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    shortCallPxsChart, col++, AveragePriceFormat, "ShortCalls", true, TextAlignment.Left, null);
                // Make 'ShortCallQty' dataGrid
                priceGridDataGridPane.AddList("PriceGrid_dataGridPane_ShortCallQty_dataGrid",
                    "ShortCallQty" + " (" + m_shortCallQtyH.LongPositions + ", " + m_shortCallQtyH.CountQty
                    + ", " + m_shortCallQtyH.OptionType + ", " + m_shortCallQtyH.CountFutures + ", " + m_shortCallQtyH.TooltipFormat
                    + ", " + m_shortCallQtyH.SigmaMult + ", " + m_shortCallQtyH.ShowNodes
                    + ") [" + option.Symbol + "]",
                    shortCallQtyChart, col++, qtyFormat, "SCQty", true, TextAlignment.Left, null);
            }
            #endregion Average price data grid

            #region Fill public properties Price, Time and IV
            DisplayPrice.Value = m_basePriceH.DisplayPrice.Value;
            DisplayTime.Value = m_dTh.Time.Value;
            DisplayIvPct.Value = m_globalSmileH.IvAtmPct.Value;

            if ((wrapDt.Count > 0) && DoubleUtil.IsPositive(basePrice))
            {
                // TODO: решить проблему формирования уникального ключа
                //object tmp = context.LoadObject("DisplaySigmaPriceWidth");
                //if ((tmp == null) || (!(tmp is double)))
                {
                    double dtYears = wrapDt[wrapDt.Count - 1];
                    if (dtYears >= 0)
                    {
                        double futPx = basePrice;
                        double sigma = m_globalSmileH.IvAtmPct.Value / Constants.PctMult;
                        double width = (sigma * Math.Sqrt(dtYears)) * futPx;

                        DisplaySigmaPriceWidth.Value = width;

                        //context.StoreObject("DisplaySigmaPriceWidth", width, false);
                    }
                }
                //else
                //{
                //    // Расчет ширины выполняю один раз и потом пользуюсь сохраненным значением.
                //    // Алгоритм по аналогии с работой блока SetViewport.
                //    double width = (double)tmp;
                //    DisplaySigmaPriceWidth.Value = width;
                //}
            }
            #endregion Fill public properties Price, Time and IV
        }
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
using CryptoBot.Exceptions;
using Infra;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CryptoBot.Tests
{
    [TestClass]
    public class CurrencyBotTests
    {
        private static readonly string s_currency = "CurrencyName";
        private static readonly decimal s_quoteOrderQuantity = 100;
        private static readonly decimal s_basePrice = 1;
        private static readonly int s_candleSize = 15;
        private static readonly DateTime s_botStartTime =  new DateTime(2020, 1, 1, 10, 10, 0);
        private static readonly DateTime s_rsiPollingEndTime = s_botStartTime.AddMinutes(10);
        private static readonly DateTime s_childStartTime = s_rsiPollingEndTime.AddMinutes(1);
        private static readonly DateTime s_validateCandleIsGreenStartTime = s_childStartTime.AddMinutes(s_candleSize-1);

        private readonly Mock<ICurrencyBotPhasesExecutor> m_currencyBotPhasesExecutorMock = new Mock<ICurrencyBotPhasesExecutor>();
        private readonly Mock<ICurrencyBotFactory> m_currencyBotFactoryMock = new Mock<ICurrencyBotFactory>();
        private readonly Mock<INotificationService> m_notificationServiceMock = new Mock<INotificationService>();
        private readonly Mock<ICryptoPriceAndRsiQueue<PriceAndRsi>> m_queueMock = new Mock<ICryptoPriceAndRsiQueue<PriceAndRsi>>();
        private readonly Mock<IAccountQuoteProvider> m_accountQuoteProvider = new Mock<IAccountQuoteProvider>();
        private readonly CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();
        private readonly PollingResponseBase m_rsiPollingResponse = new DummyPollingResponse(s_rsiPollingEndTime);

        [TestMethod]
        public async Task When_StartAsync_Given_WaitUntilLowerPriceAndHigherRsiAsync_ThrowsException_Return_FaultedBotResult()
        {
            // Arrange
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(s_botStartTime, m_cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>(),
                        It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(),
                        It.IsAny<Queue<CancellationToken>>()))
                .Throws(new Exception());

            ICurrencyBot sut = CreateCurrencyBot();
            
            // Act
           BotResultDetails botResult = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Faulted, botResult.BotResult);
            Assert.AreEqual(s_botStartTime, botResult.EndTime);
        }

        [TestMethod]
        public async Task When_StartAsync_Given_ValidateCandleIsRed_ThrowsException_Return_FaultedBotResult()
        {
            // Arrange
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(s_botStartTime, m_cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>(),
                        It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(),
                        It.IsAny<Queue<CancellationToken>>()))
                .Returns(Task.FromResult(m_rsiPollingResponse));
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsRed(s_rsiPollingEndTime, s_currency, 0,
                        2, It.IsAny<List<string>>()))
                .Throws(new Exception());

            ICurrencyBot sut = CreateCurrencyBot();

            // Act
           BotResultDetails botResult = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Faulted, botResult.BotResult);
            Assert.AreEqual(s_rsiPollingEndTime, botResult.EndTime);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_ValidateCandleIsGreen_ThrowsException_Return_FaultedBotResult()
        {
            // Arrange
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(s_botStartTime, m_cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>(),
                        It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(),
                        It.IsAny<Queue<CancellationToken>>()))
                .Returns(Task.FromResult(m_rsiPollingResponse));
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsRed(s_rsiPollingEndTime, s_currency, 0,
                        2, It.IsAny<List<string>>()))
                .Returns(true);
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsGreen(s_validateCandleIsGreenStartTime, s_currency, 0,
                        3, It.IsAny<List<string>>()))
                .Throws(new Exception());
            SetupWaitAsyncMethod(m_currencyBotPhasesExecutorMock, s_rsiPollingEndTime, m_cancellationTokenSource, s_childStartTime, 
                s_validateCandleIsGreenStartTime);
            
            ICurrencyBot sut = CreateCurrencyBot();

            // Act
           BotResultDetails botResult = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Faulted, botResult.BotResult);
            Assert.AreEqual(s_validateCandleIsGreenStartTime, botResult.EndTime);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_BuyAndPlaceSellOrder_ThrowsException_Return_FaultedBotResult()
        {
            // Arrange
            SetupAccountQuoteProvider();
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(s_botStartTime, m_cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>(),
                        It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(),
                        It.IsAny<Queue<CancellationToken>>()))
                .Returns(Task.FromResult(m_rsiPollingResponse));
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsRed(s_rsiPollingEndTime, s_currency, 0,
                        2, It.IsAny<List<string>>()))
                .Returns(true);
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsGreen(s_validateCandleIsGreenStartTime, s_currency, 0,
                        3, It.IsAny<List<string>>()))
                .Returns(true);
            m_currencyBotPhasesExecutorMock
                .Setup(m => m.BuyAndPlaceSellOrder(s_validateCandleIsGreenStartTime, 
                    s_currency, 0, 4, It.IsAny <List<string>>(), s_quoteOrderQuantity))
                .Throws(new Exception());
            SetupWaitAsyncMethod(m_currencyBotPhasesExecutorMock, s_rsiPollingEndTime, m_cancellationTokenSource, s_childStartTime, 
                s_validateCandleIsGreenStartTime);
            
            ICurrencyBot sut = CreateCurrencyBot();

            // Act
           BotResultDetails botResult = await sut.StartAsync();
            
           // Assert
           Assert.AreEqual(BotResult.Faulted, botResult.BotResult);
           Assert.AreEqual(s_validateCandleIsGreenStartTime, botResult.EndTime);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_WaitUnitPriceChangeAsync_ThrowsException_Return_FaultedBotResult()
        {
            // Arrange
            SetupAccountQuoteProvider();
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(s_botStartTime, m_cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>(),
                        It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(),
                        It.IsAny<Queue<CancellationToken>>()))
                .Returns(Task.FromResult(m_rsiPollingResponse));
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsRed(s_rsiPollingEndTime, s_currency, 0,
                        2, It.IsAny<List<string>>()))
                .Returns(true);
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsGreen(s_validateCandleIsGreenStartTime, s_currency, 0,
                        3, It.IsAny<List<string>>()))
                .Returns(true);
            var buyAndSellTradeInfo = new BuyAndSellTradeInfo(s_basePrice, 
                s_basePrice*(decimal) 1.01, s_basePrice * (decimal) 0.99, s_quoteOrderQuantity/s_basePrice);
            m_currencyBotPhasesExecutorMock
                .Setup(m => m.BuyAndPlaceSellOrder(s_validateCandleIsGreenStartTime, 
                    s_currency, 0, 4, It.IsAny <List<string>>(), s_quoteOrderQuantity))
                .Returns(Task.FromResult(buyAndSellTradeInfo));
            m_currencyBotPhasesExecutorMock
                .Setup(m => m.WaitUnitPriceChangeAsync(s_validateCandleIsGreenStartTime,
                    It.IsAny<CancellationToken>(), s_currency, s_basePrice, 0, 
                    5, It.IsAny<List<string>>()))
                .Throws(new Exception());
            SetupWaitAsyncMethod(m_currencyBotPhasesExecutorMock, s_rsiPollingEndTime, m_cancellationTokenSource, s_childStartTime, 
                s_validateCandleIsGreenStartTime);
            
            ICurrencyBot sut = CreateCurrencyBot();
            
            // Act
           BotResultDetails botResult = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Faulted, botResult.BotResult);
            Assert.AreEqual(s_validateCandleIsGreenStartTime, botResult.EndTime);
        }

        [TestMethod]
        public async Task When_StartAsync_Given_ParentWin_And_ChildLoss_Return_Win()
        {
            // Arrange
            var pricePollingEndTime = s_validateCandleIsGreenStartTime.AddMinutes(20);
            var childBotEndTime = new DateTime(2020, 1, 1, 11, 10, 0);
            var childBotDetailsResult = new BotResultDetails(BotResult.Loss, new List<string>(), childBotEndTime, s_currency);
            var pricePollingResponse = new DummyPollingResponse(pricePollingEndTime);

            SetupAccountQuoteProvider();
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(s_botStartTime, m_cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>(),
                        It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(),
                        It.IsAny<Queue<CancellationToken>>()))
                .Returns(Task.FromResult(m_rsiPollingResponse));
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsRed(s_rsiPollingEndTime, s_currency, 0,
                        2, It.IsAny<List<string>>()))
                .Returns(true);
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsGreen(s_validateCandleIsGreenStartTime, s_currency, 0,
                        3, It.IsAny<List<string>>()))
                .Returns(true);
            var buyAndSellTradeInfo = new BuyAndSellTradeInfo(s_basePrice, 
                s_basePrice*(decimal) 1.01, s_basePrice * (decimal) 0.99, s_quoteOrderQuantity/s_basePrice);
            m_currencyBotPhasesExecutorMock
                .Setup(m => m.BuyAndPlaceSellOrder(s_validateCandleIsGreenStartTime, 
                    s_currency, 0, 4, It.IsAny <List<string>>(), s_quoteOrderQuantity))
                .Returns(Task.FromResult(buyAndSellTradeInfo));
            m_currencyBotPhasesExecutorMock
                .Setup(m => m.WaitUnitPriceChangeAsync(s_validateCandleIsGreenStartTime,
                    It.IsAny<CancellationToken>(), s_currency, s_basePrice, 0, 
                    5, It.IsAny<List<string>>()))
                .Returns(Task.FromResult<(bool, PollingResponseBase)>((true,pricePollingResponse)));
            SetupChildCurrencyBotMock(childBotDetailsResult, s_childStartTime,m_currencyBotFactoryMock);
            SetupWaitAsyncMethod(m_currencyBotPhasesExecutorMock, s_rsiPollingEndTime, m_cancellationTokenSource, s_childStartTime, 
                s_validateCandleIsGreenStartTime);
            
            ICurrencyBot sut = CreateCurrencyBot();

            // Act
           BotResultDetails botResult = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Win, botResult.BotResult);
            Assert.AreEqual(pricePollingEndTime, botResult.EndTime);
        }

        private void SetupAccountQuoteProvider()
        {
            m_accountQuoteProvider.Setup(m => m.GetAvailableQuote())
                .Returns(Task.FromResult(s_quoteOrderQuantity));
        }

        [TestMethod]
        public async Task When_StartAsync_Given_RsiAndPricePolling_GotException_Return_Faulted()
        {
            // Arrange
            var gotExceptionPollingResponse = new DummyPollingResponse(s_rsiPollingEndTime, false, new Exception());
            SetupAccountQuoteProvider();
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(s_botStartTime, m_cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>(),
                        It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(),
                        It.IsAny<Queue<CancellationToken>>()))
                .Throws(new PollingResponseException(gotExceptionPollingResponse));
            
            ICurrencyBot sut = CreateCurrencyBot();

            // Act
            BotResultDetails botResult = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Faulted, botResult.BotResult);
            Assert.AreEqual(s_rsiPollingEndTime, botResult.EndTime);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_RsiAndPricePolling_GotCancelled_Return_Faulted()
        {
            // Arrange
            var gotExceptionPollingResponse = new DummyPollingResponse(s_rsiPollingEndTime, true);
            SetupAccountQuoteProvider();
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(s_botStartTime, m_cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>(), 
                        It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(), 
                        It.IsAny<Queue<CancellationToken>>()))
                .Throws(new PollingResponseException(gotExceptionPollingResponse));
            
            ICurrencyBot sut = CreateCurrencyBot();

            // Act
            BotResultDetails botResult = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Faulted, botResult.BotResult);
            Assert.AreEqual(s_rsiPollingEndTime, botResult.EndTime);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_ParentLoss_And_ChildWin_Return_Loss()
        {
            // Arrange
            var pricePollingEndTime = s_validateCandleIsGreenStartTime.AddMinutes(20);
            var childBotEndTime = new DateTime(2020, 1, 1, 11, 10, 0);
            var childBotDetailsResult = new BotResultDetails(BotResult.Win, new List<string>(), childBotEndTime, s_currency);
            SetupWaitAsyncMethod(m_currencyBotPhasesExecutorMock, s_rsiPollingEndTime, m_cancellationTokenSource, s_childStartTime, s_validateCandleIsGreenStartTime);
            var pricePollingResponse = new DummyPollingResponse(pricePollingEndTime);
            SetupAccountQuoteProvider();
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(s_botStartTime, m_cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>(),
                        It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(),
                        It.IsAny<Queue<CancellationToken>>()))
                .Returns(Task.FromResult(m_rsiPollingResponse));
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsRed(s_rsiPollingEndTime, s_currency, 0,
                        2, It.IsAny<List<string>>()))
                .Returns(true);
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsGreen(s_validateCandleIsGreenStartTime, s_currency, 0,
                        3, It.IsAny<List<string>>()))
                .Returns(true);
            var buyAndSellTradeInfo = new BuyAndSellTradeInfo(s_basePrice, 
                s_basePrice*(decimal) 1.01, s_basePrice * (decimal) 0.99, s_quoteOrderQuantity/s_basePrice);
            m_currencyBotPhasesExecutorMock
                .Setup(m => m.BuyAndPlaceSellOrder(s_validateCandleIsGreenStartTime, 
                    s_currency, 0, 4, It.IsAny <List<string>>(), s_quoteOrderQuantity))
                .Returns(Task.FromResult(buyAndSellTradeInfo));
            m_currencyBotPhasesExecutorMock
                .Setup(m => m.WaitUnitPriceChangeAsync(s_validateCandleIsGreenStartTime,
                    It.IsAny<CancellationToken>(), s_currency, s_basePrice, 0, 
                    5, It.IsAny<List<string>>()))
                .Returns(Task.FromResult<(bool, PollingResponseBase)>((false,pricePollingResponse)));
            SetupChildCurrencyBotMock(childBotDetailsResult, s_childStartTime,
                m_currencyBotFactoryMock);
            
            ICurrencyBot sut = CreateCurrencyBot();

            // Act
            BotResultDetails botResult = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Loss, botResult.BotResult);
            Assert.AreEqual(pricePollingEndTime, botResult.EndTime);
        }

        [TestMethod]
        public async Task When_StartAsync_Given_ParentFinishWithoutBuy_ChildLoss_Return_Loss()
        {
             // Arrange
            var childBotEndTime = new DateTime(2020, 1, 1, 11, 10, 0);
            var childBotDetailsResult = new BotResultDetails(BotResult.Loss, new List<string>(), childBotEndTime, s_currency);
            var childCurrencyBotMock = new Mock<ICurrencyBot>();
            SetupAccountQuoteProvider();
            childCurrencyBotMock
                .Setup(m => m.StartAsync())
                .Returns(Task.FromResult(childBotDetailsResult));
            
            SetupWaitAsyncMethod(m_currencyBotPhasesExecutorMock, s_rsiPollingEndTime, m_cancellationTokenSource, s_childStartTime,
                s_validateCandleIsGreenStartTime);

            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(s_botStartTime,
                        m_cancellationTokenSource.Token,
                        s_currency,
                        0,
                        1,
                        It.IsAny<List<string>>(),
                        It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(),
                        It.IsAny<Queue<CancellationToken>>()))
                .Returns(Task.FromResult(m_rsiPollingResponse));
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsRed(s_rsiPollingEndTime,
                        s_currency,
                        0,
                        2,
                        It.IsAny<List<string>>()))
                .Returns(true);
            m_currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsGreen(s_validateCandleIsGreenStartTime,
                        s_currency,
                        0,
                        3,
                        It.IsAny<List<string>>()))
                .Returns(false);
            SetupChildCurrencyBotMock(childBotDetailsResult, s_childStartTime,
                m_currencyBotFactoryMock);
            
            ICurrencyBot sut = CreateCurrencyBot();

            // Act
           BotResultDetails botResult = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(childBotDetailsResult.BotResult, botResult.BotResult);
            Assert.AreEqual(childBotEndTime, botResult.EndTime);
        }
        
        private CurrencyBot CreateCurrencyBot()
        {
            var sut = new CurrencyBot(m_currencyBotFactoryMock.Object,
                m_notificationServiceMock.Object,
                m_currencyBotPhasesExecutorMock.Object,
                s_currency,
                m_cancellationTokenSource,
                s_botStartTime,
                m_queueMock.Object,
                m_accountQuoteProvider.Object,
                new Queue<CancellationToken>());
            return sut;
        }

        private static void SetupChildCurrencyBotMock(BotResultDetails childBotDetailsResult,
            DateTime childStartTime,
            Mock<ICurrencyBotFactory> currencyBotFactoryMock)
        {
            const int childAge = 1;
            var childCurrencyBotMock = new Mock<ICurrencyBot>();
            childCurrencyBotMock
                .Setup(m => m.StartAsync())
                .Returns(Task.FromResult(childBotDetailsResult));
            
            currencyBotFactoryMock
                .Setup(m => m.Create(It.IsAny<ICryptoPriceAndRsiQueue<PriceAndRsi>>(), 
                    It.IsAny<Queue<CancellationToken>>(), 
                    s_currency, 
                    It.IsAny<CancellationTokenSource>(), 
                    childStartTime,
                    childAge))
                .Returns(childCurrencyBotMock.Object);
        }
        
        private static void SetupWaitAsyncMethod(Mock<ICurrencyBotPhasesExecutor> currencyBotPhasesExecutorMock, DateTime rsiPollingEndTime,
            CancellationTokenSource cancellationTokenSource, DateTime childStartTime, DateTime validateCandleIsGreenStartTime)
        {
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitAsync(rsiPollingEndTime, cancellationTokenSource.Token,
                        s_currency, 60, "WaitAfterValidateCandleIsRed"))
                .Returns(Task.FromResult(childStartTime));
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitForNextCandleAsync(childStartTime, cancellationTokenSource.Token, s_currency))
                .Returns(Task.FromResult(validateCandleIsGreenStartTime));
        }
    }

    public class DummyPollingResponse : PollingResponseBase
    {
        public DummyPollingResponse(DateTime time, bool isCancelled = false, Exception exception = null) 
            : base(time, isCancelled, exception)
        {
        }
    }
}
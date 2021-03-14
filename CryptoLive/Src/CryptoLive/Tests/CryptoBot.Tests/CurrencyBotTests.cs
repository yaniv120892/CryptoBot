using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
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
        private readonly Mock<INotificationService> m_notificationServiceMock = new Mock<INotificationService>();

        [TestMethod]
        public async Task When_StartAsync_Given_ParentWin_And_ChildLoss_Return_Win()
        {
            // Arrange
            const decimal basePrice = 1;
            var cancellationTokenSource = new CancellationTokenSource();
            var botStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            var rsiPollingEndTime = botStartTime.AddMinutes(10);
            var childStartTime = rsiPollingEndTime.AddMinutes(1);
            var validateCandleIsGreenStartTime = childStartTime.AddMinutes(14);
            var pricePollingEndTime = validateCandleIsGreenStartTime.AddMinutes(20);
            var currencyBotPhasesExecutorMock = new Mock<ICurrencyBotPhasesExecutor>();
            var childBotDetailsResult = new BotResultDetails(BotResult.Loss, new List<string>(), 99);
            var childBotEndTime = new DateTime(2020, 1, 1, 11, 10, 0);
            var currencyBotFactoryMock = SetupChildCurrencyBotMock(childBotDetailsResult, childBotEndTime, cancellationTokenSource, childStartTime);
            SetupWaitAsyncMethod(currencyBotPhasesExecutorMock, rsiPollingEndTime, cancellationTokenSource, childStartTime, validateCandleIsGreenStartTime);
            var rsiPollingResponse = new DummyPollingResponse(rsiPollingEndTime);
            var pricePollingResponse = new DummyPollingResponse(pricePollingEndTime);
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(botStartTime, cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>()))
                .Returns(Task.FromResult<PollingResponseBase>(rsiPollingResponse));
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsRed(rsiPollingEndTime, s_currency, 0,
                        2, It.IsAny<List<string>>()))
                .Returns(true);
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsGreen(validateCandleIsGreenStartTime, s_currency, 0,
                        3, It.IsAny<List<string>>()))
                .Returns(true);
            var buyAndSellTradeInfo = new BuyAndSellTradeInfo(basePrice, 
                basePrice*(decimal) 1.01, basePrice * (decimal) 0.99, s_quoteOrderQuantity/basePrice);
            currencyBotPhasesExecutorMock
                .Setup(m => m.BuyAndPlaceSellOrder(validateCandleIsGreenStartTime, 
                    s_currency, 0, 4, It.IsAny <List<string>>(), s_quoteOrderQuantity))
                .Returns(Task.FromResult(buyAndSellTradeInfo));
            currencyBotPhasesExecutorMock
                .Setup(m => m.WaitUnitPriceChangeAsync(validateCandleIsGreenStartTime,
                    It.IsAny<CancellationToken>(), s_currency, basePrice, 0, 
                    5, It.IsAny<List<string>>()))
                .Returns(Task.FromResult<(bool, PollingResponseBase)>((true,pricePollingResponse)));

            var sut = new CurrencyBot(currencyBotFactoryMock.Object, 
                m_notificationServiceMock.Object,
                currencyBotPhasesExecutorMock.Object, 
                s_currency, 
                cancellationTokenSource, 
                botStartTime,
                s_quoteOrderQuantity);

            // Act
           (BotResultDetails botResult, DateTime endTime) = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Win, botResult.BotResult);
            Assert.AreEqual(101, botResult.NewQuoteOrderQuantity);
            Assert.AreEqual(pricePollingEndTime, endTime);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_RsiAndPricePolling_GotException_Return_Faulted()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var botStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            var rsiPollingEndTime = botStartTime.AddMinutes(10);
            var currencyBotPhasesExecutorMock = new Mock<ICurrencyBotPhasesExecutor>();
            var currencyBotFactoryMock = new Mock<ICurrencyBotFactory>();
            var gotExceptionPollingResponse = new DummyPollingResponse(rsiPollingEndTime, false, new Exception());
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(botStartTime, cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>()))
                .Returns(Task.FromResult<PollingResponseBase>(gotExceptionPollingResponse));
            var sut = new CurrencyBot(currencyBotFactoryMock.Object, 
                m_notificationServiceMock.Object,
                currencyBotPhasesExecutorMock.Object, 
                s_currency, 
                cancellationTokenSource, 
                botStartTime,
                s_quoteOrderQuantity);

            // Act
           (BotResultDetails botResult, DateTime endTime) = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Faulted, botResult.BotResult);
            Assert.AreEqual(s_quoteOrderQuantity, botResult.NewQuoteOrderQuantity);
            Assert.AreEqual(rsiPollingEndTime, endTime);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_RsiAndPricePolling_GotCancelled_Return_Faulted()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var botStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            var rsiPollingEndTime = botStartTime.AddMinutes(10);
            var currencyBotPhasesExecutorMock = new Mock<ICurrencyBotPhasesExecutor>();
            var currencyBotFactoryMock = new Mock<ICurrencyBotFactory>();
            var gotExceptionPollingResponse = new DummyPollingResponse(rsiPollingEndTime, true);
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(botStartTime, cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>()))
                .Returns(Task.FromResult<PollingResponseBase>(gotExceptionPollingResponse));
            var sut = new CurrencyBot(currencyBotFactoryMock.Object, 
                m_notificationServiceMock.Object,
                currencyBotPhasesExecutorMock.Object, 
                s_currency, 
                cancellationTokenSource, 
                botStartTime,
                s_quoteOrderQuantity);

            // Act
            (BotResultDetails botResult, DateTime endTime) = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Faulted, botResult.BotResult);
            Assert.AreEqual(s_quoteOrderQuantity, botResult.NewQuoteOrderQuantity);
            Assert.AreEqual(rsiPollingEndTime, endTime);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_ParentLoss_And_ChildWin_Return_Loss()
        {
            // Arrange
            const decimal basePrice = 1;
            var cancellationTokenSource = new CancellationTokenSource();
            var botStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            var rsiPollingEndTime = botStartTime.AddMinutes(10);
            var childStartTime = rsiPollingEndTime.AddMinutes(1);
            var validateCandleIsGreenStartTime = childStartTime.AddMinutes(14);
            var pricePollingEndTime = validateCandleIsGreenStartTime.AddMinutes(20);
            var currencyBotPhasesExecutorMock = new Mock<ICurrencyBotPhasesExecutor>();
            var childBotDetailsResult = new BotResultDetails(BotResult.Win, new List<string>(), 101);
            var childBotEndTime = new DateTime(2020, 1, 1, 11, 10, 0);
            var currencyBotFactoryMock = SetupChildCurrencyBotMock(childBotDetailsResult, childBotEndTime, cancellationTokenSource, childStartTime);
            SetupWaitAsyncMethod(currencyBotPhasesExecutorMock, rsiPollingEndTime, cancellationTokenSource, childStartTime, validateCandleIsGreenStartTime);
            var rsiPollingResponse = new DummyPollingResponse(rsiPollingEndTime);
            var pricePollingResponse = new DummyPollingResponse(pricePollingEndTime);
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(botStartTime, cancellationTokenSource.Token,
                        s_currency, 0, 1, It.IsAny<List<string>>()))
                .Returns(Task.FromResult<PollingResponseBase>(rsiPollingResponse));
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsRed(rsiPollingEndTime, s_currency, 0,
                        2, It.IsAny<List<string>>()))
                .Returns(true);
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsGreen(validateCandleIsGreenStartTime, s_currency, 0,
                        3, It.IsAny<List<string>>()))
                .Returns(true);
            var buyAndSellTradeInfo = new BuyAndSellTradeInfo(basePrice, 
                basePrice*(decimal) 1.01, basePrice * (decimal) 0.99, s_quoteOrderQuantity/basePrice);
            currencyBotPhasesExecutorMock
                .Setup(m => m.BuyAndPlaceSellOrder(validateCandleIsGreenStartTime, 
                    s_currency, 0, 4, It.IsAny <List<string>>(), s_quoteOrderQuantity))
                .Returns(Task.FromResult(buyAndSellTradeInfo));
            currencyBotPhasesExecutorMock
                .Setup(m => m.WaitUnitPriceChangeAsync(validateCandleIsGreenStartTime,
                    It.IsAny<CancellationToken>(), s_currency, basePrice, 0, 
                    5, It.IsAny<List<string>>()))
                .Returns(Task.FromResult<(bool, PollingResponseBase)>((false,pricePollingResponse)));

            var sut = new CurrencyBot(currencyBotFactoryMock.Object, 
                m_notificationServiceMock.Object,
                currencyBotPhasesExecutorMock.Object, 
                s_currency, 
                cancellationTokenSource, 
                botStartTime,
                s_quoteOrderQuantity);

            // Act
           (BotResultDetails botResult, DateTime endTime) = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(BotResult.Loss, botResult.BotResult);
            Assert.AreEqual(99, botResult.NewQuoteOrderQuantity);
            Assert.AreEqual(pricePollingEndTime, endTime);
        }

        [TestMethod]
        public async Task When_StartAsync_Given_ParentNotFinished_Child1Loss_Return_Loss()
        {
             // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var botStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            var rsiPollingEndTime = botStartTime.AddMinutes(10);
            var childStartTime = rsiPollingEndTime.AddMinutes(1);
            var validateCandleIsGreenStartTime = childStartTime.AddMinutes(14);
            var currencyBotPhasesExecutorMock = new Mock<ICurrencyBotPhasesExecutor>();
            var childBotDetailsResult = new BotResultDetails(BotResult.Loss, new List<string>(), 99);
            var childBotEndTime = new DateTime(2020, 1, 1, 11, 10, 0);
            var childCurrencyBotMock = new Mock<ICurrencyBot>();
            var rsiPollingResponse = new DummyPollingResponse(rsiPollingEndTime);
            childCurrencyBotMock
                .Setup(m => m.StartAsync())
                .Returns(Task.FromResult<(BotResultDetails, DateTime)>((childBotDetailsResult, childBotEndTime)));

            var currencyBotFactoryMock = new Mock<ICurrencyBotFactory>();
            currencyBotFactoryMock
                .Setup(m => m.Create(s_currency, cancellationTokenSource, childStartTime, 100,1))
                .Returns(childCurrencyBotMock.Object);
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitAsync(rsiPollingEndTime,
                        cancellationTokenSource.Token,
                        s_currency,
                        60,
                        "FullMode_WaitBeforeStartChild"))
                .Returns(Task.FromResult(childStartTime));
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitAsync(childStartTime,
                        cancellationTokenSource.Token,
                        s_currency,
                        14*60,
                        "FullMode_WaitAfterCandleIsRed"))
                .Returns(Task.FromResult(validateCandleIsGreenStartTime));

            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitUntilLowerPriceAndHigherRsiAsync(botStartTime,
                        cancellationTokenSource.Token,
                        s_currency,
                        0,
                        1,
                        It.IsAny<List<string>>()))
                .Returns(Task.FromResult<PollingResponseBase>(rsiPollingResponse));
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsRed(rsiPollingEndTime,
                        s_currency,
                        0,
                        2,
                        It.IsAny<List<string>>()))
                .Returns(true);
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.ValidateCandleIsGreen(validateCandleIsGreenStartTime,
                        s_currency,
                        0,
                        3,
                        It.IsAny<List<string>>()))
                .Returns(false);

            var sut = new CurrencyBot(currencyBotFactoryMock.Object, 
                m_notificationServiceMock.Object,
                currencyBotPhasesExecutorMock.Object, 
                s_currency, 
                cancellationTokenSource, 
                botStartTime,
                s_quoteOrderQuantity);

            // Act
           (BotResultDetails botResult, DateTime endTime) = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(childBotDetailsResult.BotResult, botResult.BotResult);
            Assert.AreEqual(childBotDetailsResult.NewQuoteOrderQuantity, botResult.NewQuoteOrderQuantity);
            Assert.AreEqual(childBotEndTime, endTime);
        }

        private static Mock<ICurrencyBotFactory> SetupChildCurrencyBotMock(BotResultDetails childBotDetailsResult, DateTime childBotEndTime,
            CancellationTokenSource cancellationTokenSource, DateTime childStartTime)
        {
            var childCurrencyBotMock = new Mock<ICurrencyBot>();
            childCurrencyBotMock
                .Setup(m => m.StartAsync())
                .Returns(Task.FromResult<(BotResultDetails, DateTime)>((childBotDetailsResult, childBotEndTime)));

            var currencyBotFactoryMock = new Mock<ICurrencyBotFactory>();
            currencyBotFactoryMock
                .Setup(m => m.Create(s_currency, cancellationTokenSource, childStartTime, 100,1))
                .Returns(childCurrencyBotMock.Object);
            return currencyBotFactoryMock;
        }
        
        private static void SetupWaitAsyncMethod(Mock<ICurrencyBotPhasesExecutor> currencyBotPhasesExecutorMock, DateTime rsiPollingEndTime,
            CancellationTokenSource cancellationTokenSource, DateTime childStartTime, DateTime validateCandleIsGreenStartTime)
        {
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitAsync(rsiPollingEndTime, cancellationTokenSource.Token,
                        s_currency, 60, "FullMode_WaitBeforeStartChild"))
                .Returns(Task.FromResult(childStartTime));
            currencyBotPhasesExecutorMock
                .Setup(m =>
                    m.WaitAsync(childStartTime, cancellationTokenSource.Token, s_currency,
                        14 * 60, "FullMode_WaitAfterCandleIsRed"))
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
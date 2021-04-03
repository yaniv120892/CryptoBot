using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CryptoBot.Tests
{
    [TestClass]
    public class CurrencyBotPhasesExecutorTests
    {
        private const string c_currency = "MyCurrency";
        private const int c_age = 0;
        private const int c_phaseNumber = 0;
        private const int c_maxRsiToNotify = 35;
        private const int c_redCandleSize = 15;
        private const int c_greenCandleSize = 15;
        private const int c_priceChangeToNotify = 1;
        private const decimal c_buyPrice = 10;
        private const decimal c_quoteOrderQuantity = 100;
        private const long c_orderId = 1;
        private readonly Mock<ICryptoBotPhasesFactory> m_cryptoBotPhaseFactoryMock = new Mock<ICryptoBotPhasesFactory>();
        private readonly CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();
        private static readonly DateTime s_placeBuyOrderTime = new DateTime(2021, 1, 1, 0, 0, 0);
        private static readonly DateTime s_startOrderPollingTime = s_placeBuyOrderTime.AddMinutes(1);
        

        [TestMethod]
        public async Task When_BuyAndPlaceSellOrder_Given_BuyLimitOrderNotSuccess_Should_IsDoneBuyAndSellEqualsFalse()
        {
            // Arrange
            DateTime expectedEndTradeTime = s_startOrderPollingTime.AddMinutes(15);
            CurrencyBotPhasesExecutor sut = CreateCurrencyBotPhasesExecutor();
            SetupPlaceBuyLimitOrder();
            SetupNonFilledBuyLimitOrder(expectedEndTradeTime);
            SetupCancelBuyLimitOrder();
            SetupWaitBeforeOrderStatusPolling();
            
            // Act
            BuyAndSellTradeInfo buyAndPlaceSellOrder =  await sut.BuyAndPlaceSellOrder(s_placeBuyOrderTime, 
                m_cancellationTokenSource.Token, c_currency, c_age, c_phaseNumber, new List<string>(), 
                c_buyPrice, c_quoteOrderQuantity);

            // Assert
            Assert.IsFalse(buyAndPlaceSellOrder.IsDoneBuyAndSell);
            Assert.AreEqual(expectedEndTradeTime, buyAndPlaceSellOrder.EndTradeTime);
        }

        [TestMethod]
        public async Task When_BuyAndPlaceSellOrder_Given_BuyLimitOrderSuccess_Should_IsDoneBuyAndSellEqualsTrue()
        {
            // Arrange
            DateTime expectedEndTradeTime = s_startOrderPollingTime;
            CurrencyBotPhasesExecutor sut = CreateCurrencyBotPhasesExecutor();
            SetupPlaceBuyLimitOrder();
            SetupFilledBuyLimitOrder(expectedEndTradeTime);
            SetupPlaceSellOcoOrder();
            SetupWaitBeforeOrderStatusPolling();

            // Act
            BuyAndSellTradeInfo buyAndPlaceSellOrder =  await sut.BuyAndPlaceSellOrder(s_placeBuyOrderTime, 
                m_cancellationTokenSource.Token, c_currency, c_age, c_phaseNumber, new List<string>(),
                c_buyPrice, c_quoteOrderQuantity);

            // Assert
            Assert.IsTrue(buyAndPlaceSellOrder.IsDoneBuyAndSell);
            Assert.AreEqual(expectedEndTradeTime, buyAndPlaceSellOrder.EndTradeTime);
        }

        private void SetupPlaceSellOcoOrder()
        {
            decimal sellPrice = (decimal)11.2;
            decimal stopLimitPrice = (decimal) 9.92;
            Mock<ISellCryptoTrader> sellCryptoTraderMock = new Mock<ISellCryptoTrader>();
            sellCryptoTraderMock
                .Setup(m => m.SellAsync(c_currency, c_quoteOrderQuantity/c_buyPrice, sellPrice, stopLimitPrice))
                .Returns(Task.FromResult(c_orderId));
            m_cryptoBotPhaseFactoryMock.Setup(m => m.CreateOcoSellCryptoTrader())
                .Returns(sellCryptoTraderMock.Object);              
        }

        private void SetupFilledBuyLimitOrder(DateTime expectedEndTradeTime)
        {
            Mock<ICryptoPolling> cryptoPollingBaseMock = new Mock<ICryptoPolling>();
            cryptoPollingBaseMock
                .Setup(m => m.StartAsync(c_currency, m_cancellationTokenSource.Token, s_startOrderPollingTime))
                .Returns(Task.FromResult<PollingResponseBase>(new OrderPollingResponse(expectedEndTradeTime, c_orderId)));
            m_cryptoBotPhaseFactoryMock.Setup(m => m.CreateOrderStatusPolling(c_orderId))
                .Returns(cryptoPollingBaseMock.Object);        
        }

        private void SetupPlaceBuyLimitOrder()
        {
            decimal quantity = c_quoteOrderQuantity / c_buyPrice;
            Mock<IBuyCryptoTrader> buyCryptoTraderMock = new Mock<IBuyCryptoTrader>();
            buyCryptoTraderMock
                .Setup(m => m.BuyAsync(c_currency, c_buyPrice, quantity, s_placeBuyOrderTime))
                .Returns(Task.FromResult(c_orderId));
            m_cryptoBotPhaseFactoryMock.Setup(m => m.CreateStopLimitBuyCryptoTrader())
                .Returns(buyCryptoTraderMock.Object);           
        }

        private void SetupCancelBuyLimitOrder()
        {
            Mock<ICancelOrderCryptoTrader> cancelOrderCryptoTraderMock = new Mock<ICancelOrderCryptoTrader>();
            cancelOrderCryptoTraderMock
                .Setup(m => m.CancelAsync(c_currency,c_orderId))
                .Returns(Task.CompletedTask);
            m_cryptoBotPhaseFactoryMock.Setup(m => m.CreateCancelOrderCryptoTrader())
                .Returns(cancelOrderCryptoTraderMock.Object);        
        }

        private void SetupNonFilledBuyLimitOrder(DateTime expectedEndTradeTime)
        {
            Mock<ICryptoPolling> cryptoPollingBaseMock = new Mock<ICryptoPolling>();
            cryptoPollingBaseMock
                .Setup(m => m.StartAsync(c_currency, m_cancellationTokenSource.Token, s_startOrderPollingTime))
                .Returns(Task.FromResult<PollingResponseBase>(new OrderPollingResponse(expectedEndTradeTime, c_orderId, true)));
            m_cryptoBotPhaseFactoryMock.Setup(m => m.CreateOrderStatusPolling(c_orderId))
                .Returns(cryptoPollingBaseMock.Object);
        }
        
        private void SetupWaitBeforeOrderStatusPolling()
        {
            m_cryptoBotPhaseFactoryMock
                .Setup(m => m.SystemClock.Wait(It.IsAny<CancellationToken>(), 
                    c_currency, 60, "WaitBeforeStartPollingOrderStatus", s_placeBuyOrderTime))
                .Returns(Task.FromResult(s_startOrderPollingTime));
        }

        private CurrencyBotPhasesExecutor CreateCurrencyBotPhasesExecutor()
        {
            return new CurrencyBotPhasesExecutor(m_cryptoBotPhaseFactoryMock.Object,
                c_maxRsiToNotify, 
                c_redCandleSize, 
                c_greenCandleSize, 
                c_priceChangeToNotify);
        }
    }
}
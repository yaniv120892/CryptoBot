using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Abstractions;
using CryptoBot.CryptoPollings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Services.Abstractions;
using Utils.Abstractions;
using Utils.SystemClocks;

namespace CryptoBot.Tests.CryptoPollings
{
    [TestClass]
    public class OrderCryptoPollingTests
    {
        private const string c_currency = "CurrencyName";
        private const string c_filledOrderStatus = "Filled";
        private const string c_newOrderStatus = "New";
        private readonly long s_orderId = 1;
        private static readonly DateTime s_pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
        private readonly ISystemClock m_systemClock = new DummySystemClock();
        private readonly Mock<ITradeService> m_tradeServiceMock = new Mock<ITradeService>();
        private readonly CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();

        [TestMethod]
        public async Task When_StartAsync_Given_OrderFilled_Should_Should_SuccessEqualsTrue()
        {
            // Arrange
            var sut = new OrderCryptoPolling(m_systemClock, m_tradeServiceMock.Object, s_orderId);
            m_tradeServiceMock.Setup(m => m.GetOrderStatusAsync(c_currency, s_orderId, s_pollingStartTime))
                .Returns(Task.FromResult(c_filledOrderStatus));

            // Act
            PollingResponseBase pollingResponseBase =
                await sut.StartAsync(c_currency, m_cancellationTokenSource.Token, s_pollingStartTime);

            // Assert
            Assert.IsTrue(pollingResponseBase.IsSuccess);
            Assert.AreEqual(s_pollingStartTime, pollingResponseBase.Time);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_OrderFilledBeforeTimeoutReached_Should_SuccessEqualsTrue()
        {
            // Arrange
            DateTime expectedEndPollingTime = s_pollingStartTime.AddMinutes(2);
            var sut = new OrderCryptoPolling(m_systemClock, m_tradeServiceMock.Object, s_orderId);
            m_tradeServiceMock.SetupSequence(m => m.GetOrderStatusAsync(c_currency, s_orderId, It.IsAny<DateTime>()))
                .Returns(Task.FromResult(c_newOrderStatus))
                .Returns(Task.FromResult(c_newOrderStatus))
                .Returns(Task.FromResult(c_filledOrderStatus));
            
            // Act
            PollingResponseBase pollingResponseBase =
                await sut.StartAsync(c_currency, m_cancellationTokenSource.Token, s_pollingStartTime);

            // Assert
            Assert.IsTrue(pollingResponseBase.IsSuccess);
            Assert.AreEqual(expectedEndPollingTime, pollingResponseBase.Time);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_OrderNotFilledAndTimeoutReached_Should_IsCanceledEqualsTrue()
        {
            // Arrange
            DateTime expectedEndPollingTime = s_pollingStartTime.AddMinutes(15);
            var sut = new OrderCryptoPolling(m_systemClock, m_tradeServiceMock.Object, s_orderId);
            m_tradeServiceMock.Setup(m => m.GetOrderStatusAsync(c_currency, s_orderId, It.IsAny<DateTime>()))
                .Returns(Task.FromResult(c_newOrderStatus));
            
            // Act
            PollingResponseBase pollingResponseBase =
                await sut.StartAsync(c_currency, m_cancellationTokenSource.Token, s_pollingStartTime);

            // Assert
            Assert.IsFalse(pollingResponseBase.IsSuccess);
            Assert.IsNull(pollingResponseBase.Exception);
            Assert.IsTrue(pollingResponseBase.IsCancelled);
            Assert.AreEqual(expectedEndPollingTime, pollingResponseBase.Time);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_OrderNotFilledAndExceptionThrownAfter2Minutes_Should_ExceptionNotEqualsNull()
        {
            // Arrange
            DateTime expectedEndPollingTime = s_pollingStartTime.AddMinutes(2);
            var sut = new OrderCryptoPolling(m_systemClock, m_tradeServiceMock.Object, s_orderId);
            m_tradeServiceMock.SetupSequence(m => m.GetOrderStatusAsync(c_currency, s_orderId, It.IsAny<DateTime>()))
                .Returns(Task.FromResult(c_newOrderStatus))
                .Returns(Task.FromResult(c_newOrderStatus))
                .Throws(new Exception());
            
            // Act
            PollingResponseBase pollingResponseBase =
                await sut.StartAsync(c_currency, m_cancellationTokenSource.Token, s_pollingStartTime);

            // Assert
            Assert.IsFalse(pollingResponseBase.IsSuccess);
            Assert.IsNotNull(pollingResponseBase.Exception);
            Assert.IsFalse(pollingResponseBase.IsCancelled);
            Assert.AreEqual(expectedEndPollingTime, pollingResponseBase.Time);
        }
    }
}
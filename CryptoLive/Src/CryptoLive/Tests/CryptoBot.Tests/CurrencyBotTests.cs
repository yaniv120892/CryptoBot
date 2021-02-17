using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CryptoBot.Tests
{
    [TestClass]
    public class CurrencyBotTests
    {
        private static readonly string s_currency = "CurrencyName";
        [TestMethod]
        public void When_StartAsync_Given_ParentWin_And_ChildLoss_Return_Win()
        {
            // Arrange
            var botStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            var currencyBotPhasesExecutorMock = new Mock<ICurrencyBotPhasesExecutor>();
            var sut = new CurrencyBot(currencyBotPhasesExecutorMock.Object, s_currency, new CancellationTokenSource(), botStartTime, 0);
            
            // Act
            sut.StartAsync();
            // Assert
        }
        
        [TestMethod]
        public void When_StartAsync_Given_ParentLoss_And_ChildWin_Return_Loss()
        {
            
        }
        
        [TestMethod]
        public void When_StartAsync_Given_ParentNotFinished_Child1Loss_Return_Loss()
        {
            
        }
        
        [TestMethod]
        public void When_StartAsync_Given_ParentNotFinished_Child1NotFinish_Child2Win_Return_Win()
        {
            
        }
        
        [TestMethod]
        public void When_StartAsync_Given_ParentNotFinished_Child1NotFinish_Child2Loss_Return_Loss()
        {
            
        }
    }
}
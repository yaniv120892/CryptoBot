using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using CryptoBot.Abstractions.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CryptoBot.Tests
{
    [TestClass]
    public class CurrencyBotTests
    {
        private static readonly string s_currency = "CurrencyName";
        [TestMethod]
        public async Task When_StartAsync_Given_ParentWin_And_ChildLoss_Return_Win()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var botStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            var currencyBotPhasesExecutorMock = new Mock<ICurrencyBotPhasesExecutor>();
            var childBotDetailsResult = new BotResultDetails(BotResult.Loss, new List<string>());
            var childBotEndTime = new DateTime(2020, 1, 1, 11, 10, 0);
            var childCurrencyBotMock = new Mock<CurrencyBot>();
            childCurrencyBotMock
                .Setup(m => m.StartAsync())
                .Returns(Task.FromResult<(BotResultDetails, DateTime)>((childBotDetailsResult, childBotEndTime)));

            var currencyBotFactoryMock = new Mock<ICurrencyBotFactory>();
            currencyBotFactoryMock
                .Setup(m => m.Create(s_currency, cancellationTokenSource, botStartTime, 1))
                .Returns(childCurrencyBotMock.Object);
            
            
            var sut = new CurrencyBot(currencyBotFactoryMock.Object, currencyBotPhasesExecutorMock.Object, s_currency, new CancellationTokenSource(), botStartTime);

            // Act
           (BotResultDetails botResult, DateTime endTime) = await sut.StartAsync();
            
            // Assert
            Assert.AreEqual(botResult.BotResult, BotResult.Win);
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
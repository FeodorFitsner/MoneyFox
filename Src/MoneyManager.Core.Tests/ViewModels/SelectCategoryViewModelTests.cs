﻿using Cirrious.MvvmCross.Test.Core;
using MoneyManager.Core.ViewModels;
using MoneyManager.Foundation.Model;
using MoneyManager.Foundation.OperationContracts;
using Moq;
using Xunit;

namespace MoneyManager.Core.Tests.ViewModels
{
    public class SelectCategoryViewModelTests : MvxIoCSupportingTest
    {
        public SelectCategoryViewModelTests()
        {
            Setup();
        }

        [Fact]
        public void ResetCategoryCommand_FilledProperty_PropertyIsNull()
        {
            var transactionSetup = new Mock<ITransactionRepository>();

            var transaction = new FinancialTransaction
            {
                Category = new Category()
            };

            transactionSetup.SetupGet(x => x.Selected).Returns(transaction);
            var transactionRepository = transactionSetup.Object;

            new SelectCategoryViewModel(transactionRepository).ResetCategoryCommand.Execute();

            Assert.Null(transaction.Category);
        }
    }
}
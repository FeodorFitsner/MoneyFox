﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MoneyFox.Business.ViewModels;
using MoneyFox.Foundation.DataModels;
using MoneyFox.Foundation.Interfaces;
using MoneyFox.Foundation.Interfaces.Repositories;
using MoneyFox.Foundation.Tests;
using Moq;
using MvvmCross.Test.Core;
using Xunit;

namespace MoneyFox.Business.Tests.ViewModels
{
    [Collection("MvxIocCollection")]
    public class AccountListViewModelTests : MvxIoCSupportingTest
    {
        private Mock<IAccountRepository> accountRepository;

        public AccountListViewModelTests()
        {
            accountRepository = new Mock<IAccountRepository>();
            accountRepository.SetupAllProperties();
        }

        [Fact]
        public void DeleteAccountCommand_UserReturnTrue_ExecuteDeletion()
        {
            var deleteCalled = false;

            accountRepository.Setup(x => x.Delete(It.IsAny<AccountViewModel>())).Callback(() => deleteCalled = true);

            var endofMonthManagerSetup = new Mock<IEndOfMonthManager>();
            var dialogServiceSetup = new Mock<IDialogService>();
            dialogServiceSetup.Setup(x => x.ShowConfirmMessage(It.IsAny<string>(), It.IsAny<string>(), null, null))
                .Returns(Task.FromResult(true));

            var settingsManagerMock = new Mock<ISettingsManager>();

            var viewModel = new AccountListViewModel(accountRepository.Object, new Mock<IPaymentManager>().Object,
                dialogServiceSetup.Object, endofMonthManagerSetup.Object, settingsManagerMock.Object,
                new Mock<IModifyDialogService>().Object);

            viewModel.DeleteAccountCommand.Execute(new AccountViewModel {Id = 3});

            deleteCalled.ShouldBeTrue();
        }

        [Fact]
        public void DeleteAccountCommand_UserReturnFalse_SkipDeletion()
        {
            var deleteCalled = false;
            accountRepository.Setup(x => x.Delete(It.IsAny<AccountViewModel>())).Callback(() => deleteCalled = true);
            var endofMonthManagerSetup = new Mock<IEndOfMonthManager>();

            var settingsManagerMock = new Mock<ISettingsManager>();

            var dialogServiceSetup = new Mock<IDialogService>();
            dialogServiceSetup.Setup(x => x.ShowConfirmMessage(It.IsAny<string>(), It.IsAny<string>(), null, null))
                .Returns(Task.FromResult(false));

            var viewModel = new AccountListViewModel(accountRepository.Object, new Mock<IPaymentManager>().Object,
                dialogServiceSetup.Object, endofMonthManagerSetup.Object, settingsManagerMock.Object,
                new Mock<IModifyDialogService>().Object);

            viewModel.DeleteAccountCommand.Execute(new AccountViewModel {Id = 3});

            deleteCalled.ShouldBeFalse();
        }

        [Fact]
        public void DeleteAccountCommand_AccountNull_DoNothing()
        {
            var deleteCalled = false;

            accountRepository.Setup(x => x.Delete(It.IsAny<AccountViewModel>())).Callback(() => deleteCalled = true);
            var endofMonthManagerSetup = new Mock<IEndOfMonthManager>();

            var dialogServiceSetup = new Mock<IDialogService>();
            dialogServiceSetup.Setup(x => x.ShowConfirmMessage(It.IsAny<string>(), It.IsAny<string>(), null, null))
                .Returns(Task.FromResult(true));

            var settingsManagerMock = new Mock<ISettingsManager>();

            var viewModel = new AccountListViewModel(accountRepository.Object, new Mock<IPaymentManager>().Object,
                dialogServiceSetup.Object, endofMonthManagerSetup.Object, settingsManagerMock.Object,
                new Mock<IModifyDialogService>().Object);

            viewModel.DeleteAccountCommand.Execute(null);

            deleteCalled.ShouldBeFalse();
        }

        [Fact]
        public void IsAllAccountsEmpty_AccountsEmpty_True()
        {
            var settingsManagerMock = new Mock<ISettingsManager>();
            var endofMonthManagerSetup = new Mock<IEndOfMonthManager>();
            accountRepository.Setup(x => x.GetList(null)).Returns(new List<AccountViewModel>());
            var vm = new AccountListViewModel(accountRepository.Object, new Mock<IPaymentManager>().Object, null,
                endofMonthManagerSetup.Object, settingsManagerMock.Object, new Mock<IModifyDialogService>().Object);
            vm.LoadedCommand.Execute();
            vm.IsAllAccountsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void IsAllAccountsEmpty_OneAccount_False()
        {
            var settingsManagerMock = new Mock<ISettingsManager>();
            accountRepository.SetupSequence(x => x.GetList(It.IsAny<Expression<Func<AccountViewModel, bool>>>()))
                .Returns(new List<AccountViewModel>
                {
                    new AccountViewModel()
                });
            var endofMonthManagerSetup = new Mock<IEndOfMonthManager>();
            var vm = new AccountListViewModel(accountRepository.Object, new Mock<IPaymentManager>().Object, null,
                endofMonthManagerSetup.Object, settingsManagerMock.Object, new Mock<IModifyDialogService>().Object);
            vm.LoadedCommand.Execute();
            vm.IsAllAccountsEmpty.ShouldBeFalse();
        }

        [Fact]
        public void IsAllAccountsEmpty_TwoAccount_False()
        {
            var settingsManagerMock = new Mock<ISettingsManager>();
            var endofMonthManagerSetup = new Mock<IEndOfMonthManager>();
            accountRepository.Setup(x => x.GetList(It.IsAny<Expression<Func<AccountViewModel, bool>>>()))
                .Returns(new List<AccountViewModel>
                {
                    new AccountViewModel(),
                    new AccountViewModel()
                });

            var vm = new AccountListViewModel(accountRepository.Object, new Mock<IPaymentManager>().Object, null,
                endofMonthManagerSetup.Object, settingsManagerMock.Object, new Mock<IModifyDialogService>().Object);
            vm.LoadedCommand.Execute();
            vm.IsAllAccountsEmpty.ShouldBeFalse();
        }

        [Fact]
        public void IsAllAccountsEmpty_ExcludedAccountsSet_False()
        {
            var settingsManagerMock = new Mock<ISettingsManager>();
            var endofMonthManagerSetup = new Mock<IEndOfMonthManager>();
            accountRepository.SetupSequence(x => x.GetList(It.IsAny<Expression<Func<AccountViewModel, bool>>>()))
                .Returns(new List<AccountViewModel>())
                .Returns(new List<AccountViewModel>
                {
                    new AccountViewModel {IsExcluded = true},
                });

            var vm = new AccountListViewModel(accountRepository.Object, new Mock<IPaymentManager>().Object, null,
                endofMonthManagerSetup.Object, settingsManagerMock.Object, new Mock<IModifyDialogService>().Object);
            vm.LoadedCommand.Execute();
            vm.IsAllAccountsEmpty.ShouldBeFalse();
        }

        [Fact]
        public void IncludedAccounts_AccountsAvailable_MatchesRepository()
        {
            var settingsManagerMock = new Mock<ISettingsManager>();
            var endofMonthManagerSetup = new Mock<IEndOfMonthManager>();
            accountRepository.Setup(x => x.GetList(It.IsAny<Expression<Func<AccountViewModel, bool>>>()))
                .Returns(new List<AccountViewModel>
                {
                    new AccountViewModel {Id = 22},
                    new AccountViewModel {Id = 33},
                });
            var vm = new AccountListViewModel(accountRepository.Object, new Mock<IPaymentManager>().Object, null,
                endofMonthManagerSetup.Object, settingsManagerMock.Object, new Mock<IModifyDialogService>().Object);

            vm.LoadedCommand.Execute();
            vm.IncludedAccounts.Count.ShouldBe(2);
            vm.IncludedAccounts[0].Id.ShouldBe(22);
            vm.IncludedAccounts[1].Id.ShouldBe(33);
        }

        [Fact]
        public void ExcludedAccounts_AccountsAvailable_MatchesRepository()
        {
            var settingsManagerMock = new Mock<ISettingsManager>();
            var endofMonthManagerSetup = new Mock<IEndOfMonthManager>();
            accountRepository.SetupSequence(x => x.GetList(It.IsAny<Expression<Func<AccountViewModel, bool>>>()))
                .Returns(new List<AccountViewModel>())
                .Returns(new List<AccountViewModel>
                {
                    new AccountViewModel {Id = 22},
                    new AccountViewModel {Id = 33}
                });
            var vm = new AccountListViewModel(accountRepository.Object, new Mock<IPaymentManager>().Object, null,
                endofMonthManagerSetup.Object, settingsManagerMock.Object, new Mock<IModifyDialogService>().Object);

            vm.LoadedCommand.Execute();
            vm.ExcludedAccounts.Count.ShouldBe(2);
            vm.ExcludedAccounts[0].Id.ShouldBe(22);
            vm.ExcludedAccounts[1].Id.ShouldBe(33);
        }

        [Fact]
        public void IncludedAccounts_NoAccountsAvailable_MatchesRepository()
        {
            var settingsManagerMock = new Mock<ISettingsManager>();
            var endofMonthManagerSetup = new Mock<IEndOfMonthManager>();
            accountRepository.Setup(x => x.GetList(null)).Returns(new List<AccountViewModel>());
            accountRepository.Setup(x => x.GetList(It.IsAny<Expression<Func<AccountViewModel, bool>>>()))
                .Returns(new List<AccountViewModel>());
            var vm = new AccountListViewModel(accountRepository.Object, new Mock<IPaymentManager>().Object, null,
                endofMonthManagerSetup.Object, settingsManagerMock.Object, new Mock<IModifyDialogService>().Object);
            vm.LoadedCommand.Execute();
            vm.IncludedAccounts.Any().ShouldBeFalse();
            vm.ExcludedAccounts.Any().ShouldBeFalse();
        }
    }
}
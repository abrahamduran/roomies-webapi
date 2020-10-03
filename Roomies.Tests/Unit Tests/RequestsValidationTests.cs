using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Roomies.Tests.Mocks;
using Xunit;

namespace Roomies.Tests.UnitTests
{
    public class RequestsValidationTests
    {
        [Fact]
        public void CreateRoommate_IsInvalid_WhenNameIsNull()
        {
            // arrange
            var roommate = Mock.Requests.Roommate(name: null);

            // act
            var results = ValidateModel(roommate);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(roommate.Name)));
        }

        [Fact]
        public void CreateRoommate_IsInvalid_WhenNameIsLargerThan30Characters()
        {
            // arrange
            var roommate = Mock.Requests.Roommate();
            for (int i = 0; i < 50; i++)
                roommate.Name += "a";

            // act
            var results = ValidateModel(roommate);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(roommate.Name)));
        }

        [Fact]
        public void CreateRoommate_IsInvalid_WhenEmailIsNull()
        {
            // arrange
            var roommate = Mock.Requests.Roommate(email: null);

            // act
            var results = ValidateModel(roommate);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(roommate.Email)));
        }

        [Fact]
        public void CreateRoommate_IsInvalid_WhenEmailIsLargerThan30Characters()
        {
            // arrange
            var roommate = Mock.Requests.Roommate();
            for (int i = 0; i < 50; i++)
                roommate.Email += "a";

            // act
            var results = ValidateModel(roommate);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(roommate.Email)));
        }

        [Fact]
        public void CreateRoommate_IsInvalid_WhenEmailIsNotAValidEmail()
        {
            // arrange
            var roommate = Mock.Requests.Roommate(email: "wrong-email");

            // act
            var results = ValidateModel(roommate);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(roommate.Email)));
        }

        [Fact]
        public void IndexAutocomplete_IsInvalid_WhenTextIsNull()
        {
            // arrange
            var autocomplete = Mock.Requests.Autocomplete(text: null);

            // act
            var results = ValidateModel(autocomplete);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(autocomplete.Text)));
        }

        [Fact]
        public void IndexAutocomplete_IsInvalid_WhenTextIsLargerThan30Characters()
        {
            // arrange
            var autocomplete = Mock.Requests.Autocomplete();
            for (int i = 0; i < 50; i++)
                autocomplete.Text += "a";

            // act
            var results = ValidateModel(autocomplete);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(autocomplete.Text)));
        }

        [Fact]
        public void RegisterPayment_IsInvalid_WhenExpenseIdsIsEmpty()
        {
            // arrange
            var payment = Mock.Requests.Payment(expenseIds: new string[0]);

            // act
            var results = ValidateModel(payment);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(payment.ExpenseIds)));
        }

        [Fact]
        public void RegisterPayment_IsInvalid_WhenExpenseIdsIsNull()
        {
            // arrange
            var payment = Mock.Requests.Payment();
            payment.ExpenseIds = null;

            // act
            var results = ValidateModel(payment);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(payment.ExpenseIds)));
        }

        [Fact]
        public void RegisterPayment_IsInvalid_WhenPaidToIsNull()
        {
            // arrange
            var payment = Mock.Requests.Payment();
            payment.PaidTo = null;

            // act
            var results = ValidateModel(payment);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(payment.PaidTo)));
        }

        [Fact]
        public void RegisterPayment_IsInvalid_WhenPaidByIsNull()
        {
            // arrange
            var payment = Mock.Requests.Payment();
            payment.PaidBy = null;

            // act
            var results = ValidateModel(payment);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(payment.PaidBy)));
        }

        [Fact]
        public void RegisterPayment_IsInvalid_WhenNegativeAmountIsProvided()
        {
            // arrange
            var payment = Mock.Requests.Payment(amount: -100);

            // act
            var results = ValidateModel(payment);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(payment.Amount)));
        }

        [Fact]
        public void RegisterPayment_IsInvalid_WhenDescriptionIsLongerThan100Characters()
        {
            // arrange
            var payment = Mock.Requests.Payment();
            for (int i = 0; i < 200; i++)
                payment.Description += "a";

            // act
            var results = ValidateModel(payment);

            // assert
            Assert.NotEmpty(results);
            Assert.Contains(results, v => v.MemberNames.Contains(nameof(payment.Description)));
        }

        private List<ValidationResult> ValidateModel<T>(T model)
        {
            var context = new ValidationContext(model, null, null);
            var result = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, result, true);

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using ShoppingSystem.Controllers;
using ShoppingSystem.Models;
using ShoppingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ShoppingSystem.Tests
{
    public class CustomersControllerTests
    {
        private List<Customer> GetTestCustomers()
        {
            var users = new List<Customer>
            {
                new Customer {Id=1, FirstName = "Ramil", LastName = "Naum", Address = "Los-Ang", Discount = "5"},
                new Customer {Id=2, FirstName = "Bob", LastName = "Dillan", Address = "Berlin", Discount = "7"},
                new Customer {Id=3, FirstName = "Kile", LastName = "Rise", Address = "London", Discount = "0"},
                new Customer {Id=4, FirstName = "John", LastName = "Konor", Address = "Vashington", Discount = "3"},
            };
            return users;
        }

        [Fact]
        public async Task Details_NonExistingId_ReturnsBadRequest()
        {
            // Arrange
            int testCustomerId = 0;
            var mockRepo = new Mock<ICustomers>();
            mockRepo.Setup(repo => repo.GetByIdAsync(testCustomerId))
                .ThrowsAsync(new Exception());
            var controller = new CustomersController(mockRepo.Object);

            // Act
            var result = await controller.Details(testCustomerId);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Details_ExistingId_ReturnsViewResultWithCustomer()
        {
            // Arrange
            int testCustomerId = 1;
            var mockRepo = new Mock<ICustomers>();
            mockRepo.Setup(repo => repo.GetByIdAsync(testCustomerId))
                .ReturnsAsync(GetTestCustomers().FirstOrDefault(c => c.Id == testCustomerId));
            var controller = new CustomersController(mockRepo.Object);

            // Act
            var result = await controller.Details(testCustomerId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Customer>(viewResult.ViewData.Model);
            Assert.Equal(testCustomerId, model.Id);
            Assert.Equal("Ramil", model.FirstName);
            Assert.Equal("Naum", model.LastName);
            Assert.Equal("Los-Ang", model.Address);
            Assert.Equal("5", model.Discount);
        }

        [Fact]
        public async Task Index_SearchStringIsNull_ReturnsAListOf4Customers()
        {
            // Arrange 
            string testString = null;
            var mock = new Mock<ICustomers>();
            mock.Setup(customers => customers.GetAllAsync()).ReturnsAsync(GetTestCustomers());
            var controller = new CustomersController(mock.Object);

            // Act
            var result = await controller.Index(testString) as ViewResult;

            // Assert
            var model = Assert.IsAssignableFrom<IEnumerable<Customer>>(result.ViewData.Model);
            Assert.Equal(4, model.Count());
        }
        [Fact]
        public async Task Index_SearchStringIsEmpty_ReturnsAListOf4Customers()
        {
            // Arrange 
            string testString = "";
            SortState testState = SortState.AddressDesc;
            var mock = new Mock<ICustomers>();
            mock.Setup(customers => customers.GetAllAsync()).ReturnsAsync(GetTestCustomers());
            var controller = new CustomersController(mock.Object);

            // Act
            var result = await controller.Index(testString,testState) as ViewResult;

            // Assert
            var model = Assert.IsAssignableFrom<IEnumerable<Customer>>(result.ViewData.Model);
            Assert.Equal(4, model.Count());
        }
            [Fact]
        public async Task Index_ReturnsViewResult_WithAlistOfUsers()
        {
            // Arrange 
            string testString = "il";
            SortState testState = SortState.LastNameAsc;
            var mock = new Mock<ICustomers>();
            mock.Setup(customers => customers.GetAllAsync()).ReturnsAsync(GetTestCustomers());
            var controller = new CustomersController(mock.Object);

            // Act
            var result = await controller.Index(testString,testState);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model=Assert.IsAssignableFrom<IEnumerable<Customer>>(viewResult.ViewData.Model);
            Assert.Equal(3, model.Count());
        }
        [Fact]
        public async Task Index_ViewData_ReturnsMessage()
        {
            // Arrange
            string testString = "il";
            SortState testState = SortState.LastNameAsc;
            var mock = new Mock<ICustomers>();
            mock.Setup(customers => customers.GetAllAsync()).ReturnsAsync(GetTestCustomers());
            var controller = new CustomersController(mock.Object);

            // Act
            var result = await controller.Index(testString,testState) as ViewResult;

            // Assert
            Assert.Equal(testString, result.ViewData["CurrentFilter"]);
            Assert.Equal(SortState.LastNameDesc, result.ViewData["LastNameSort"]);
            Assert.Equal(SortState.AddressAsc, result.ViewData["AddressSort"]);
        }

        [Fact]
        public void Create_ReturnsViewResult()
        {
            // Arrange 
            var mock = new Mock<ICustomers>();
            var controller = new CustomersController(mock.Object);

            // Act
            var result = controller.Create();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_ModelStateIsValid_ReturnsRedirectAndAddCustomer()
        {
            // Arrange 
            var mock = new Mock<ICustomers>();
            mock.Setup(repo => repo.AddAsync(It.IsAny<Customer>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var controller = new CustomersController(mock.Object);
            var newCustomer= new Customer() { Id = 1, FirstName = "Jack", LastName = "Sparrow" };

            // Act
            var result = await controller.Create(newCustomer);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            mock.Verify(r => r.AddAsync(newCustomer));
        }

        [Fact]
        public async Task Create_ModelStateIsInvalid_ReturnsViewResultWithCustomer()
        {
            // Arrange 
            var mockRepo = new Mock<ICustomers>();
            mockRepo.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(GetTestCustomers());
            var controller = new CustomersController(mockRepo.Object);
            controller.ModelState.AddModelError("Id", "Required");
            var newCustomer = new Customer();

            // Act
            var result = await controller.Create(newCustomer);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Customer>(viewResult.ViewData.Model);
            Assert.Equal(newCustomer, model);
        }

        [Fact]
        public async Task Edit_CustomerIsNull_ReturnsNotFoundResult()
        {
            // Arrange 
            int testCustomerId = 1;
            var mock = new Mock<ICustomers>();
            mock.Setup(customers => customers.GetByIdAsync(testCustomerId)).ReturnsAsync((Customer)null);
            var controller = new CustomersController(mock.Object);

            // Act
            var result =await controller.Edit(testCustomerId);

            //Assert 
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_CustomerFound_ReturnsViewResultWithCustomer()
        {
            // Arrange 
            int testCustomerId = 1;
            var mock = new Mock<ICustomers>();
            mock.Setup(customers=>customers.GetByIdAsync(testCustomerId)).ReturnsAsync(GetTestCustomers().FirstOrDefault(c=>c.Id==testCustomerId));
            var controller = new CustomersController(mock.Object);

            //Act
            var result = await controller.Edit(testCustomerId);

            //Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Customer>(viewResult.ViewData.Model);
            Assert.Equal(testCustomerId,model.Id);
            Assert.Equal("Ramil",model.FirstName);
            Assert.Equal("Naum",model.LastName);
            Assert.Equal("Los-Ang",model.Address);
            Assert.Equal("5",model.Discount);
        }

        [Fact]
        public async Task Edit_InvalidCustomerId_ReturnsNotFoundResult()
        {
            // Arrange 
            int testCustomerId = 1;
            var mock = new Mock<ICustomers>();
            var controller = new CustomersController(mock.Object);
            var editCustomer = new Customer() { Id = 0 };

            // Act
            var result = await controller.Edit(testCustomerId, editCustomer);

            //Assert 
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_ConcurrencyException_ReturnsBadRequest()
        {
            // Arrange
            int testCustomerId = 1;
            var editCustomer = new Customer() { Id = 1 };
            var mockRepo = new Mock<ICustomers>();
            mockRepo.Setup(repo => repo.EditAsync(editCustomer))
                .ThrowsAsync(new DbUpdateConcurrencyException());
            var controller = new CustomersController(mockRepo.Object);

            // Act
            var result = await controller.Edit(testCustomerId, editCustomer);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Edit_ValidCustomerAndId_ReturnsRedirectAndEditCustomer()
        {
            // Arrange
            int testCustomerId = 1;
            var editCustomer = new Customer() { Id = 1 };
            var mockRepo = new Mock<ICustomers>();
            mockRepo.Setup(repo => repo.EditAsync(It.IsAny<Customer>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var controller = new CustomersController(mockRepo.Object);

            // Act
            var result = await controller.Edit(testCustomerId, editCustomer);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            mockRepo.Verify(r => r.EditAsync(editCustomer));
        }

        [Fact]
        public async Task Edit_ModelStateIsInvalid_ReturnsViewResultWithCustomer()
        {
            // Arrange 
            int testCustomerId = 1;
            var editCustomer = new Customer() { Id = 1 };
            var mockRepo = new Mock<ICustomers>();
            var controller = new CustomersController(mockRepo.Object);
            controller.ModelState.AddModelError("FirstName", "Required");

            // Act
            var result = await controller.Edit(testCustomerId, editCustomer);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Customer>(viewResult.ViewData.Model);
            Assert.Equal(editCustomer, model);
        }

        [Fact]
        public async Task Delete_CustomerIsNull_ReturnsNotFoundResult()
        {
            // Arrange 
            int testCustomerId = 1;
            var mock = new Mock<ICustomers>();
            mock.Setup(customers => customers.GetByIdAsync(testCustomerId)).ReturnsAsync((Customer)null);
            var controller = new CustomersController(mock.Object);

            // Act
            var result = await controller.Delete(testCustomerId);

            //Assert 
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_CustomerFound_ReturnsViewResultWithCustomer()
        {
            // Arrange 
            int testCustomerId = 1;
            var mock = new Mock<ICustomers>();
            mock.Setup(customers => customers.GetByIdAsync(testCustomerId)).ReturnsAsync(GetTestCustomers().FirstOrDefault(c => c.Id == testCustomerId));
            var controller = new CustomersController(mock.Object);

            //Act
            var result = await controller.Delete(testCustomerId);

            //Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Customer>(viewResult.ViewData.Model);
            Assert.Equal(testCustomerId, model.Id);
            Assert.Equal("Ramil", model.FirstName);
            Assert.Equal("Naum", model.LastName);
            Assert.Equal("Los-Ang", model.Address);
            Assert.Equal("5", model.Discount);
        }

        [Fact]
        public async Task Delete_CustomerIsValid_ReturnsRedirectAndDeleteCustomer()
        {
            // Arrange 
            int testCustomerId = 1;
            var mock = new Mock<ICustomers>();
            mock.Setup(customers => customers.DeleteAsync(testCustomerId))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var controller = new CustomersController(mock.Object);

            // Act
            var result = await controller.DeleteConfirmed(testCustomerId);

            //Assert 
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            mock.Verify(r => r.DeleteAsync(testCustomerId));
        }

        [Fact]
        public async Task Delete_CustomerIsInvalid_ReturnsViewResult()
        {
            // Arrange 
            int testCustomerId = 1;
            var mock = new Mock<ICustomers>();
            mock.Setup(customers => customers.DeleteAsync(testCustomerId))
                .ThrowsAsync(new Exception());
            var controller = new CustomersController(mock.Object);

            // Act
            var result = await controller.DeleteConfirmed(testCustomerId);

            //Assert 
            Assert.IsType<ViewResult>(result);
        }

    }
}

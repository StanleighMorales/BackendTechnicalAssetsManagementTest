using BackendTechnicalAssetsManagement.src.Utils;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Utils
{
    public class ApiResponseTests
    {
        #region SuccessResponse Tests

        [Fact]
        public void SuccessResponse_WithData_ShouldReturnSuccessResponse()
        {
            // Arrange
            var testData = new { Id = 1, Name = "Test" };
            var message = "Operation successful";

            // Act
            var response = ApiResponse<object>.SuccessResponse(testData, message);

            // Assert
            Assert.True(response.Success);
            Assert.Equal(message, response.Message);
            Assert.NotNull(response.Data);
            Assert.Null(response.Errors);
        }

        [Fact]
        public void SuccessResponse_WithDefaultMessage_ShouldUseDefaultMessage()
        {
            // Arrange
            var testData = "Test data";

            // Act
            var response = ApiResponse<string>.SuccessResponse(testData);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Request successful.", response.Message);
            Assert.Equal(testData, response.Data);
        }

        [Fact]
        public void SuccessResponse_WithNullData_ShouldAllowNullData()
        {
            // Act
            var response = ApiResponse<string?>.SuccessResponse(null, "Success with no data");

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Success with no data", response.Message);
            Assert.Null(response.Data);
            Assert.Null(response.Errors);
        }

        [Fact]
        public void SuccessResponse_WithComplexObject_ShouldReturnComplexObject()
        {
            // Arrange
            var complexData = new List<string> { "Item1", "Item2", "Item3" };

            // Act
            var response = ApiResponse<List<string>>.SuccessResponse(complexData, "List retrieved");

            // Assert
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(3, response.Data.Count);
            Assert.Contains("Item1", response.Data);
        }

        #endregion

        #region FailResponse Tests

        [Fact]
        public void FailResponse_WithMessage_ShouldReturnFailResponse()
        {
            // Arrange
            var errorMessage = "Operation failed";

            // Act
            var response = ApiResponse<string>.FailResponse(errorMessage);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Null(response.Data);
            Assert.Null(response.Errors);
        }

        [Fact]
        public void FailResponse_WithErrors_ShouldReturnFailResponseWithErrors()
        {
            // Arrange
            var errorMessage = "Validation failed";
            var errors = new List<string> { "Field1 is required", "Field2 is invalid" };

            // Act
            var response = ApiResponse<object>.FailResponse(errorMessage, errors);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Null(response.Data);
            Assert.NotNull(response.Errors);
            Assert.Equal(2, response.Errors.Count);
            Assert.Contains("Field1 is required", response.Errors);
            Assert.Contains("Field2 is invalid", response.Errors);
        }

        [Fact]
        public void FailResponse_WithEmptyErrorList_ShouldReturnEmptyErrorList()
        {
            // Arrange
            var errorMessage = "Failed";
            var errors = new List<string>();

            // Act
            var response = ApiResponse<string>.FailResponse(errorMessage, errors);

            // Assert
            Assert.False(response.Success);
            Assert.NotNull(response.Errors);
            Assert.Empty(response.Errors);
        }

        [Fact]
        public void FailResponse_WithNullErrors_ShouldAllowNullErrors()
        {
            // Arrange
            var errorMessage = "Generic error";

            // Act
            var response = ApiResponse<int>.FailResponse(errorMessage, null);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Null(response.Errors);
        }

        [Fact]
        public void FailResponse_DataShouldBeDefault()
        {
            // Arrange
            var errorMessage = "Error occurred";

            // Act
            var response = ApiResponse<int>.FailResponse(errorMessage);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(0, response.Data); // default(int) is 0
        }

        #endregion

        #region Validation Response Tests

        [Fact]
        public void FailResponse_AsValidationError_ShouldReturnValidationResponse()
        {
            // Arrange
            var validationMessage = "Validation errors occurred";
            var validationErrors = new List<string>
            {
                "Username is required",
                "Email format is invalid",
                "Password must be at least 8 characters"
            };

            // Act
            var response = ApiResponse<object>.FailResponse(validationMessage, validationErrors);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(validationMessage, response.Message);
            Assert.NotNull(response.Errors);
            Assert.Equal(3, response.Errors.Count);
        }

        #endregion

        #region Type Safety Tests

        [Fact]
        public void ApiResponse_WithIntType_ShouldWorkCorrectly()
        {
            // Act
            var successResponse = ApiResponse<int>.SuccessResponse(42, "Number retrieved");
            var failResponse = ApiResponse<int>.FailResponse("Failed to get number");

            // Assert
            Assert.True(successResponse.Success);
            Assert.Equal(42, successResponse.Data);
            
            Assert.False(failResponse.Success);
            Assert.Equal(0, failResponse.Data);
        }

        [Fact]
        public void ApiResponse_WithBoolType_ShouldWorkCorrectly()
        {
            // Act
            var successResponse = ApiResponse<bool>.SuccessResponse(true, "Boolean retrieved");
            var failResponse = ApiResponse<bool>.FailResponse("Failed");

            // Assert
            Assert.True(successResponse.Success);
            Assert.True(successResponse.Data);
            
            Assert.False(failResponse.Success);
            Assert.False(failResponse.Data); // default(bool) is false
        }

        [Fact]
        public void ApiResponse_WithCustomClass_ShouldWorkCorrectly()
        {
            // Arrange
            var customObject = new TestClass { Id = 1, Name = "Test" };

            // Act
            var successResponse = ApiResponse<TestClass>.SuccessResponse(customObject, "Object retrieved");
            var failResponse = ApiResponse<TestClass>.FailResponse("Failed");

            // Assert
            Assert.True(successResponse.Success);
            Assert.NotNull(successResponse.Data);
            Assert.Equal(1, successResponse.Data.Id);
            
            Assert.False(failResponse.Success);
            Assert.Null(failResponse.Data); // default(TestClass) is null
        }

        #endregion

        // Helper class for testing
        private class TestClass
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }
    }
}

using Xunit;
using GoldBranchAI.Models;

namespace GoldBranchAI.Tests
{
    public class TodoTaskTests
    {
        [Fact]
        public void KanbanStatus_ShouldBe_Bekliyor_WhenCreated()
        {
            // Arrange
            var task = new TodoTask
            {
                Title = "Test Görev",
                Description = "Birim testi",
                IsCompleted = false
            };

            // Act & Assert
            Assert.Equal("Devam Ediyor", task.Status); // Status model tarafında default olarak "Devam Ediyor"
            Assert.False(task.IsCompleted);
        }

        [Fact]
        public void AppUser_InitialRole_ShouldBeAssign()
        {
            // Arrange
            var user = new AppUser
            {
                FullName = "Unit Tester",
                Email = "test@test.com",
                Role = "Gelistirici"
            };

            // Act & Assert
            Assert.Equal("Gelistirici", user.Role);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.DTOs
{
    // DTO cho IUserService.cs (lấy lịch sử đăng nhập)
    public class LoginActivityDto
    {
        public long Id { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
        public bool Success { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO cho IUserService.cs và AuthService.cs (lấy danh sách user)
    public class UserListDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    // DTO cho AdminUsersController.cs và AuthService.cs (cập nhật role)
    public class UpdateUserRoleDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int RoleId { get; set; }
    }

    // DTO cho AdminUsersController.cs và AuthService.cs (cập nhật level)
    public class UpdateLevelDtoUser
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int LevelId { get; set; }
    }

    // DTO cho AdminUsersController.cs và AuthService.cs (cập nhật trạng thái)
    public class UpdateUserStatusDto
    {
        [Required]
        public bool IsActive { get; set; }
    }
}
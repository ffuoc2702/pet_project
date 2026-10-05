using Microsoft.AspNetCore.Identity;

namespace MatchingSports.Infrastructure.Identity;

public sealed class VietnameseIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Create(nameof(DefaultError), "Đã xảy ra lỗi khi xử lý thông tin tài khoản.");
    public override IdentityError ConcurrencyFailure() => Create(nameof(ConcurrencyFailure), "Dữ liệu đã thay đổi. Vui lòng tải lại và thử lại.");
    public override IdentityError PasswordMismatch() => Create(nameof(PasswordMismatch), "Mật khẩu không chính xác.");
    public override IdentityError InvalidToken() => Create(nameof(InvalidToken), "Mã xác thực không hợp lệ.");
    public override IdentityError RecoveryCodeRedemptionFailed() => Create(nameof(RecoveryCodeRedemptionFailed), "Mã khôi phục không hợp lệ hoặc đã được sử dụng.");
    public override IdentityError LoginAlreadyAssociated() => Create(nameof(LoginAlreadyAssociated), "Thông tin đăng nhập này đã được liên kết với tài khoản khác.");
    public override IdentityError InvalidUserName(string? userName) => Create(nameof(InvalidUserName), "Tên đăng nhập không hợp lệ hoặc chứa ký tự không được phép.");
    public override IdentityError InvalidEmail(string? email) => Create(nameof(InvalidEmail), "Địa chỉ email không hợp lệ.");
    public override IdentityError DuplicateUserName(string userName) => Create(nameof(DuplicateUserName), "Tên đăng nhập đã được sử dụng.");
    public override IdentityError DuplicateEmail(string email) => Create(nameof(DuplicateEmail), "Địa chỉ email đã được sử dụng.");
    public override IdentityError InvalidRoleName(string? role) => Create(nameof(InvalidRoleName), "Tên vai trò không hợp lệ.");
    public override IdentityError DuplicateRoleName(string role) => Create(nameof(DuplicateRoleName), "Tên vai trò đã được sử dụng.");
    public override IdentityError UserAlreadyHasPassword() => Create(nameof(UserAlreadyHasPassword), "Tài khoản đã có mật khẩu.");
    public override IdentityError UserLockoutNotEnabled() => Create(nameof(UserLockoutNotEnabled), "Chưa bật tính năng khóa tài khoản này.");
    public override IdentityError UserAlreadyInRole(string role) => Create(nameof(UserAlreadyInRole), "Người dùng đã được gán vai trò này.");
    public override IdentityError UserNotInRole(string role) => Create(nameof(UserNotInRole), "Người dùng chưa được gán vai trò này.");
    public override IdentityError PasswordTooShort(int length) => Create(nameof(PasswordTooShort), $"Mật khẩu phải có ít nhất {length} ký tự.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Create(nameof(PasswordRequiresUniqueChars), $"Mật khẩu phải có ít nhất {uniqueChars} ký tự khác nhau.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Create(nameof(PasswordRequiresNonAlphanumeric), "Mật khẩu phải có ít nhất một ký tự đặc biệt.");
    public override IdentityError PasswordRequiresDigit() => Create(nameof(PasswordRequiresDigit), "Mật khẩu phải có ít nhất một chữ số từ 0 đến 9.");
    public override IdentityError PasswordRequiresLower() => Create(nameof(PasswordRequiresLower), "Mật khẩu phải có ít nhất một chữ cái thường từ a đến z.");
    public override IdentityError PasswordRequiresUpper() => Create(nameof(PasswordRequiresUpper), "Mật khẩu phải có ít nhất một chữ cái hoa từ A đến Z.");

    private static IdentityError Create(string code, string description) => new() { Code = code, Description = description };
}

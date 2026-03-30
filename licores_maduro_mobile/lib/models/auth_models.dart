// API usa PropertyNamingPolicy = null → devuelve PascalCase
class LoginResponse {
  final String token;
  final UserInfo user;

  LoginResponse({required this.token, required this.user});

  factory LoginResponse.fromJson(Map<String, dynamic> json) => LoginResponse(
        token: json['Token'] as String,
        user: UserInfo.fromJson(json['User'] as Map<String, dynamic>),
      );
}

class UserInfo {
  final int id;
  final String username;
  final String fullName;
  final int roleId;
  final String roleName;
  final String? avatarUrl;

  UserInfo({
    required this.id,
    required this.username,
    required this.fullName,
    required this.roleId,
    required this.roleName,
    this.avatarUrl,
  });

  factory UserInfo.fromJson(Map<String, dynamic> json) => UserInfo(
        id: json['UserId'] as int? ?? 0,
        username: json['Username'] as String? ?? '',
        fullName: json['FullName'] as String? ?? json['Username'] as String? ?? '',
        roleId: json['RoleId'] as int? ?? 0,
        roleName: json['RoleName'] as String? ?? '',
        avatarUrl: json['AvatarUrl'] as String?,
      );
}

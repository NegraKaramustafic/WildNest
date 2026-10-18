import 'package:ecommerce_desktop/utils/json_parsers.dart';

class AppUser {
  final int id;
  final String firstName;
  final String lastName;
  final String email;
  final String username;
  final String role;
  final bool isActive;
  final DateTime? createdAt;
  final DateTime? lastLoginAt;
  final String? phoneNumber;
  final String? profileImageBase64;

  const AppUser({
    required this.id,
    this.firstName = '',
    this.lastName = '',
    this.email = '',
    this.username = '',
    this.role = '',
    this.isActive = true,
    this.createdAt,
    this.lastLoginAt,
    this.phoneNumber,
    this.profileImageBase64,
  });

  String get fullName {
    final name = '$firstName $lastName'.trim();
    return name.isEmpty ? username : name;
  }

  factory AppUser.fromJson(Map<String, dynamic> json) {
    return AppUser(
      id: asInt(json['id']),
      firstName: asString(json['firstName']),
      lastName: asString(json['lastName']),
      email: asString(json['email']),
      username: asString(json['username']),
      role: asString(json['role']),
      isActive: json['isActive'] as bool? ?? true,
      createdAt: parseDateTime(json['createdAt']),
      lastLoginAt: parseDateTime(json['lastLoginAt']),
      phoneNumber: blankToNull(json['phoneNumber'] as String?),
      profileImageBase64: blankToNull(json['profileImageBase64'] as String?),
    );
  }
}

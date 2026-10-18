String? requiredText(String? value, String label, {int max = 100, int min = 1}) {
  final text = value?.trim() ?? '';
  if (text.isEmpty) return '$label is required.';
  if (text.length < min) return '$label must be at least $min characters.';
  if (text.length > max) return '$label cannot exceed $max characters.';
  return null;
}

String? optionalText(String? value, String label, int max) {
  final text = value?.trim() ?? '';
  if (text.length > max) return '$label cannot exceed $max characters.';
  return null;
}

String? emailError(String? value) {
  final text = value?.trim() ?? '';
  if (text.isEmpty) return 'Email is required.';
  if (text.length > 100) return 'Email cannot exceed 100 characters.';
  final email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
  if (!email.hasMatch(text)) return 'Email must be a valid email address.';
  return null;
}

String? phoneError(String? value) {
  final text = value?.trim() ?? '';
  if (text.isEmpty) return null;
  if (!RegExp(r'^[0-9+\-() ]{6,20}$').hasMatch(text)) {
    return 'Phone number must be 6 to 20 characters and may contain only digits, spaces, and + - ( ).';
  }
  return null;
}

String? passwordError({
  required bool required,
  required String? password,
  required String? confirm,
}) {
  final next = password?.trim() ?? '';
  final confirmation = confirm?.trim() ?? '';
  if (!required && next.isEmpty && confirmation.isEmpty) return null;
  if (next.isEmpty && confirmation.isNotEmpty) {
    return 'Enter a new password, or leave both password fields blank.';
  }
  if (next.isEmpty) return 'Password is required.';
  if (next.length < 6) return 'Password must be at least 6 characters.';
  if (next.length > 100) return 'Password cannot exceed 100 characters.';
  if (next != confirmation) return 'Password confirmation does not match the new password.';
  return null;
}

String? confirmPasswordError({
  required bool required,
  required String? password,
  required String? confirm,
}) {
  final next = password?.trim() ?? '';
  final confirmation = confirm?.trim() ?? '';
  if (!required && next.isEmpty && confirmation.isEmpty) return null;
  if (confirmation != next) return 'Password confirmation does not match the new password.';
  return null;
}

String? goalAmountError(String? value) {
  final text = value?.trim() ?? '';
  if (text.isEmpty) return 'Goal amount is required.';
  final amount = double.tryParse(text);
  if (amount == null) return 'Goal amount must be a number greater than 0.';
  if (amount <= 0) return 'Goal amount must be greater than 0.';
  return null;
}

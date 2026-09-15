import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
import 'package:flutter/material.dart';

void alertBox(BuildContext context, String title, String content) {
  showDialog(
    context: context,
    builder: (context) => AlertDialog(
      title: Text(title),
      content: Text(content),
      actions: [
        ElevatedButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('OK'),
        ),
      ],
    ),
  );
}

bool shouldIgnoreError(Object error) => error is SessionExpiredException;

void showAppError(BuildContext context, Object error) {
  if (shouldIgnoreError(error) || !context.mounted) {
    return;
  }
  alertBox(context, 'Error', error.toString());
}

InputDecoration authInputDecoration({
  required String label,
  required IconData icon,
  Widget? suffix,
}) {
  return InputDecoration(
    labelText: label,
    prefixIcon: Icon(icon, color: AppColors.sage),
    suffixIcon: suffix,
  );
}

const String mandatoryField = 'This field is required';

import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:flutter/material.dart';

class AsyncBody extends StatelessWidget {
  const AsyncBody({
    super.key,
    required this.loading,
    required this.child,
    this.error,
    this.onRetry,
    this.isEmpty = false,
    this.emptyMessage = 'Nothing to show yet.',
  });

  final bool loading;
  final Widget child;
  final String? error;
  final VoidCallback? onRetry;
  final bool isEmpty;
  final String emptyMessage;

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const Center(
        child: CircularProgressIndicator(color: AppColors.sage),
      );
    }
    if (error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                error!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textMuted),
              ),
              if (onRetry != null) ...[
                const SizedBox(height: 12),
                ElevatedButton(onPressed: onRetry, child: const Text('Try again')),
              ],
            ],
          ),
        ),
      );
    }
    if (isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            emptyMessage,
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppColors.textMuted),
          ),
        ),
      );
    }
    return child;
  }
}

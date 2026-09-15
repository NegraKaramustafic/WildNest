import 'package:ecommerce_mobile/providers/review_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

Future<bool?> showLeaveReviewDialog(
  BuildContext context, {
  required int activityId,
}) {
  return showDialog<bool>(
    context: context,
    useRootNavigator: false,
    barrierColor: Colors.black.withValues(alpha: 0.45),
    builder: (_) => LeaveReviewDialog(activityId: activityId),
  );
}

class LeaveReviewDialog extends StatefulWidget {
  const LeaveReviewDialog({super.key, required this.activityId});

  final int activityId;

  @override
  State<LeaveReviewDialog> createState() => _LeaveReviewDialogState();
}

class _LeaveReviewDialogState extends State<LeaveReviewDialog> {
  final _comment = TextEditingController();
  int _rating = 0;
  bool _submitting = false;

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  bool get _canSubmit =>
      _rating >= 1 && _comment.text.trim().isNotEmpty && !_submitting;

  Future<void> _submit() async {
    if (!_canSubmit) return;
    setState(() => _submitting = true);
    try {
      await context.read<ReviewProvider>().insert({
        'activityId': widget.activityId,
        'rating': _rating,
        'comment': _comment.text.trim(),
      });
      if (!mounted) return;
      Navigator.pop(context, true);
    } on ApiClientException catch (e) {
      if (!mounted) return;
      final already = e.message.toLowerCase().contains('already reviewed');
      if (already) {
        Navigator.pop(context, true);
        return;
      }
      alertBox(context, 'Error', e.message);
    } catch (e) {
      if (!mounted) return;
      showAppError(context, e);
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: AppColors.surface,
      insetPadding: const EdgeInsets.symmetric(horizontal: 22, vertical: 24),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 16, 12, 20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Leave a review',
                    textAlign: TextAlign.center,
                    style: GoogleFonts.playfairDisplay(
                      fontSize: 22,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                IconButton(
                  onPressed: () => Navigator.pop(context, false),
                  icon: const Icon(Icons.close, color: AppColors.textMuted),
                ),
              ],
            ),
            const SizedBox(height: 8),
            const Text(
              'Rating',
              style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
            ),
            const SizedBox(height: 6),
            Row(
              children: List.generate(5, (index) {
                final value = index + 1;
                final filled = value <= _rating;
                return IconButton(
                  onPressed: () => setState(() => _rating = value),
                  padding: EdgeInsets.zero,
                  constraints: const BoxConstraints(minWidth: 36, minHeight: 36),
                  icon: Icon(
                    filled ? Icons.star_rounded : Icons.star_border_rounded,
                    size: 32,
                    color: filled ? AppColors.orange : AppColors.border,
                  ),
                );
              }),
            ),
            const SizedBox(height: 8),
            const Text(
              'Comment',
              style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _comment,
              maxLines: 4,
              maxLength: 500,
              onChanged: (_) => setState(() {}),
              decoration: InputDecoration(
                hintText: 'Write your comment...',
                counterText: '',
                filled: true,
                fillColor: AppColors.surface,
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(16),
                  borderSide: const BorderSide(color: AppColors.sage, width: 1.2),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(16),
                  borderSide: const BorderSide(color: AppColors.sage, width: 1.6),
                ),
              ),
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _canSubmit ? _submit : null,
                style: ElevatedButton.styleFrom(
                  backgroundColor: _canSubmit
                      ? AppColors.sage
                      : AppColors.mintDeep,
                  foregroundColor: _canSubmit
                      ? Colors.white
                      : AppColors.sageMuted,
                  disabledBackgroundColor: AppColors.mintDeep,
                  disabledForegroundColor: AppColors.sageMuted,
                ),
                child: _submitting
                    ? const SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Text('Submit'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

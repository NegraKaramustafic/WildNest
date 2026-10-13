import 'package:ecommerce_mobile/models/ticket.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:flutter/material.dart';

class TicketVerificationResult extends StatelessWidget {
  const TicketVerificationResult({
    super.key,
    this.ticket,
    this.error,
    this.errorTitle = 'Ticket rejected',
    this.loading = false,
    this.onNext,
    this.nextLabel = 'Scan next',
  });

  final Ticket? ticket;
  final String? error;
  final String errorTitle;
  final bool loading;
  final VoidCallback? onNext;
  final String nextLabel;

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const _StatusBanner(
        tone: _ResultTone.success,
        title: 'Checking ticket...',
        message: 'Hold on while the ticket is verified.',
        loading: true,
      );
    }

    if (error != null) {
      return _StatusBanner(
        tone: _ResultTone.error,
        title: errorTitle,
        message: error!,
        onNext: onNext,
        nextLabel: nextLabel,
      );
    }

    final verified = ticket;
    if (verified == null) {
      return const SizedBox.shrink();
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const _StatusBanner(
          tone: _ResultTone.success,
          title: 'Ticket verified',
          message: 'This ticket is valid and has been marked as used.',
        ),
        const SizedBox(height: 12),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: AppColors.border),
          ),
          child: Column(
            children: [
              _DetailRow(label: 'Code', value: _orDash(verified.code)),
              _DetailRow(label: 'Visitor', value: _orDash(verified.userFullName)),
              _DetailRow(
                label: 'Visit date',
                value: formatReviewDate(verified.visitDate),
              ),
              _DetailRow(
                label: 'Visitors',
                value: visitorLabel(verified.visitorCount),
              ),
              _DetailRow(label: 'Total', value: formatMoney(verified.totalPrice)),
              if (verified.verifiedAt != null)
                _DetailRow(
                  label: 'Verified at',
                  value: _formatVerifiedAt(verified.verifiedAt!),
                ),
              if (verified.verifiedByFullName != null &&
                  verified.verifiedByFullName!.trim().isNotEmpty)
                _DetailRow(
                  label: 'Verified by',
                  value: verified.verifiedByFullName!.trim(),
                ),
            ],
          ),
        ),
        if (onNext != null) ...[
          const SizedBox(height: 14),
          ElevatedButton(onPressed: onNext, child: Text(nextLabel)),
        ],
      ],
    );
  }
}

String _orDash(String value) {
  final trimmed = value.trim();
  return trimmed.isEmpty ? '—' : trimmed;
}

String _formatVerifiedAt(DateTime value) {
  final local = value.toLocal();
  final clock = formatClock(Duration(hours: local.hour, minutes: local.minute));
  return '${formatReviewDate(local)} · $clock';
}

enum _ResultTone { success, error }

class _ToneStyle {
  const _ToneStyle({
    required this.background,
    required this.border,
    required this.foreground,
    required this.icon,
  });

  final Color background;
  final Color border;
  final Color foreground;
  final IconData icon;

  static _ToneStyle of(_ResultTone tone) {
    switch (tone) {
      case _ResultTone.success:
        return const _ToneStyle(
          background: AppColors.mint,
          border: AppColors.sage,
          foreground: AppColors.sageDark,
          icon: Icons.check_circle_rounded,
        );
      case _ResultTone.error:
        return _ToneStyle(
          background: AppColors.heart.withValues(alpha: 0.12),
          border: AppColors.heart,
          foreground: AppColors.heart,
          icon: Icons.error_outline_rounded,
        );
    }
  }
}

class _StatusBanner extends StatelessWidget {
  const _StatusBanner({
    required this.tone,
    required this.title,
    required this.message,
    this.loading = false,
    this.onNext,
    this.nextLabel = 'Scan next',
  });

  final _ResultTone tone;
  final String title;
  final String message;
  final bool loading;
  final VoidCallback? onNext;
  final String nextLabel;

  @override
  Widget build(BuildContext context) {
    final style = _ToneStyle.of(tone);
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: style.background,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: style.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (loading)
                Padding(
                  padding: const EdgeInsets.only(top: 2),
                  child: SizedBox(
                    width: 22,
                    height: 22,
                    child: CircularProgressIndicator(
                      strokeWidth: 2.4,
                      color: style.foreground,
                    ),
                  ),
                )
              else
                Icon(style.icon, color: style.foreground),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: TextStyle(
                        fontWeight: FontWeight.w800,
                        fontSize: 16,
                        color: style.foreground,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      message,
                      style: TextStyle(
                        color: style.foreground,
                        height: 1.35,
                        fontWeight: FontWeight.w500,
                        fontSize: 14,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          if (onNext != null) ...[
            const SizedBox(height: 14),
            ElevatedButton(onPressed: onNext, child: Text(nextLabel)),
          ],
        ],
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 108,
            child: Text(label, style: const TextStyle(color: AppColors.textMuted)),
          ),
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
          ),
        ],
      ),
    );
  }
}

import 'package:ecommerce_desktop/models/ticket.dart';
import 'package:ecommerce_desktop/providers/ticket_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class TicketVerificationScreen extends StatefulWidget {
  const TicketVerificationScreen({super.key});

  @override
  State<TicketVerificationScreen> createState() => _TicketVerificationScreenState();
}

class _TicketVerificationScreenState extends State<TicketVerificationScreen> {
  final _codeController = TextEditingController();
  bool _verifying = false;
  Ticket? _ticket;
  String? _error;

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _verify() async {
    final code = _codeController.text.trim();
    if (code.isEmpty) {
      setState(() {
        _ticket = null;
        _error = 'Unesite kod ulaznice.';
      });
      return;
    }

    setState(() {
      _verifying = true;
      _ticket = null;
      _error = null;
    });

    try {
      final ticket = await context.read<TicketProvider>().verifyManualCode(code);
      if (!mounted) return;
      setState(() {
        _ticket = ticket;
        _verifying = false;
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.toString().replaceFirst('Bad state: ', '');
        _verifying = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(28),
      child: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 760),
          child: ListView(
            children: [
              const Text(
                'Verifikacija ulaznica',
                style: TextStyle(
                  fontSize: 26,
                  fontWeight: FontWeight.w700,
                  color: AppColors.text,
                ),
              ),
              const SizedBox(height: 6),
              const Text(
                'Unesite kod ulaznice za provjeru valjanosti',
                style: TextStyle(color: AppColors.textMuted),
              ),
              const SizedBox(height: 18),
              Container(
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: AppColors.border),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Kod ulaznice',
                      style: TextStyle(fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: TextField(
                            controller: _codeController,
                            textInputAction: TextInputAction.done,
                            onSubmitted: (_) => _verify(),
                            decoration: const InputDecoration(
                              hintText: 'npr. WN-A1B2C3',
                              prefixIcon: Icon(Icons.confirmation_number_outlined),
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        FilledButton(
                          onPressed: _verifying ? null : _verify,
                          child: Text(_verifying ? 'Provjera...' : 'Verificiraj'),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              TicketVerifyResultView(ticket: _ticket, error: _error),
            ],
          ),
        ),
      ),
    );
  }
}

class TicketVerifyResultView extends StatelessWidget {
  const TicketVerifyResultView({super.key, this.ticket, this.error});

  final Ticket? ticket;
  final String? error;

  @override
  Widget build(BuildContext context) {
    if (error != null) {
      return _ResultCard(
        tone: _ResultTone.error,
        title: 'Ulaznica nije verificirana',
        message: error!,
      );
    }

    final verified = ticket;
    if (verified == null) {
      return const SizedBox.shrink();
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const _ResultCard(
          tone: _ResultTone.success,
          title: 'Ulaznica je uspješno verificirana',
          message: 'Kod je prihvaćen i ulaznica je označena kao iskorištena.',
        ),
        if (verified.warning != null && verified.warning!.isNotEmpty) ...[
          const SizedBox(height: 12),
          _ResultCard(
            tone: _ResultTone.warning,
            title: 'Upozorenje',
            message: verified.warning!,
          ),
        ],
        const SizedBox(height: 12),
        Container(
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: AppColors.border),
          ),
          child: Column(
            children: [
              _DetailRow(label: 'Kod', value: verified.code),
              _DetailRow(label: 'Posjetilac', value: verified.userFullName),
              _DetailRow(label: 'Datum posjete', value: formatDate(verified.visitDate)),
              _DetailRow(label: 'Broj posjetilaca', value: verified.visitorCount.toString()),
              _DetailRow(label: 'Ukupno', value: formatKm(verified.totalPrice)),
              _DetailRow(label: 'Status', value: verified.statusLabel),
              if (verified.verifiedAt != null)
                _DetailRow(
                  label: 'Vrijeme verifikacije',
                  value: formatDate(verified.verifiedAt!.toLocal()),
                ),
              if (verified.verifiedByFullName != null &&
                  verified.verifiedByFullName!.isNotEmpty)
                _DetailRow(label: 'Verificirao', value: verified.verifiedByFullName!),
            ],
          ),
        ),
      ],
    );
  }
}

enum _ResultTone { success, warning, error }

class _ResultCard extends StatelessWidget {
  const _ResultCard({
    required this.tone,
    required this.title,
    required this.message,
  });

  final _ResultTone tone;
  final String title;
  final String message;

  @override
  Widget build(BuildContext context) {
    final Color background;
    final Color border;
    final Color foreground;
    final IconData icon;
    switch (tone) {
      case _ResultTone.success:
        background = AppColors.successBg;
        border = AppColors.successBorder;
        foreground = AppColors.successText;
        icon = Icons.check_circle_outline;
      case _ResultTone.warning:
        background = AppColors.warningBg;
        border = AppColors.warningBorder;
        foreground = AppColors.warningText;
        icon = Icons.warning_amber_outlined;
      case _ResultTone.error:
        background = AppColors.errorBg;
        border = AppColors.errorBorder;
        foreground = AppColors.errorText;
        icon = Icons.error_outline;
    }

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: border),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: foreground),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: TextStyle(fontWeight: FontWeight.w700, color: foreground),
                ),
                const SizedBox(height: 4),
                Text(message, style: TextStyle(color: foreground, height: 1.35)),
              ],
            ),
          ),
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
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          SizedBox(
            width: 180,
            child: Text(label, style: const TextStyle(color: AppColors.textMuted)),
          ),
          Expanded(
            child: Text(value, style: const TextStyle(fontWeight: FontWeight.w600)),
          ),
        ],
      ),
    );
  }
}

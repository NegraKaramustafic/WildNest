import 'package:ecommerce_mobile/models/ticket.dart';
import 'package:ecommerce_mobile/providers/ticket_provider.dart';
import 'package:ecommerce_mobile/screens/tickets/ticket_payment_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class TicketSummaryScreen extends StatefulWidget {
  const TicketSummaryScreen({
    super.key,
    required this.visitDate,
    required this.visitorCount,
  });

  final DateTime visitDate;
  final int visitorCount;

  @override
  State<TicketSummaryScreen> createState() => _TicketSummaryScreenState();
}

class _TicketSummaryScreenState extends State<TicketSummaryScreen> {
  bool _busy = false;
  Ticket? _created;

  double get _previewTotal => zooEntryPricePerPerson * widget.visitorCount;

  Future<void> _continueToPayment() async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      final provider = context.read<TicketProvider>();
      _created ??= await provider.createPending(
        visitDate: widget.visitDate,
        visitorCount: widget.visitorCount,
      );
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => TicketPaymentScreen(ticket: _created!),
        ),
      );
      if (mounted) setState(() => _busy = false);
    } catch (e) {
      if (!mounted) return;
      setState(() => _busy = false);
      showAppError(context, e);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          children: [
            WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
                children: [
                  Text(
                    'Review booking',
                    style: GoogleFonts.playfairDisplay(
                      fontSize: 32,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 18),
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(24),
                    ),
                    child: Row(
                      children: [
                        Container(
                          width: 56,
                          height: 56,
                          decoration: BoxDecoration(
                            color: AppColors.mint,
                            borderRadius: BorderRadius.circular(16),
                          ),
                          child: const Icon(
                            Icons.confirmation_number_outlined,
                            color: AppColors.sage,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'WildNest Entry Ticket',
                                style: TextStyle(
                                  fontWeight: FontWeight.w700,
                                  fontSize: 16,
                                ),
                              ),
                              Text(
                                formatReviewDate(widget.visitDate),
                                style: const TextStyle(color: AppColors.textMuted),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 14),
                  Row(
                    children: [
                      Expanded(
                        child: _MiniStat(
                          label: 'Date',
                          value: formatDayMonth(widget.visitDate),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: _MiniStat(
                          label: 'Visitors',
                          value: '${widget.visitorCount}',
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 14),
                  Container(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(24),
                    ),
                    child: Column(
                      children: [
                        _PriceRow(
                          label: 'Price per person',
                          value: formatMoney(zooEntryPricePerPerson),
                        ),
                        const Divider(color: AppColors.border),
                        _PriceRow(
                          label: 'Number of visitors',
                          value: '×${widget.visitorCount}',
                        ),
                        const Divider(color: AppColors.border),
                        _PriceRow(
                          label: 'Total',
                          value: formatMoney(_previewTotal),
                          emphasize: true,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 20),
              child: ElevatedButton(
                onPressed: _busy ? null : _continueToPayment,
                child: _busy
                    ? const SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Text('Continue to payment'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MiniStat extends StatelessWidget {
  const _MiniStat({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Column(
        children: [
          Text(label, style: const TextStyle(color: AppColors.textMuted, fontSize: 13)),
          const SizedBox(height: 6),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18)),
        ],
      ),
    );
  }
}

class _PriceRow extends StatelessWidget {
  const _PriceRow({
    required this.label,
    required this.value,
    this.emphasize = false,
  });

  final String label;
  final String value;
  final bool emphasize;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        children: [
          Text(
            label,
            style: TextStyle(
              fontWeight: emphasize ? FontWeight.w700 : FontWeight.w500,
              color: emphasize ? AppColors.text : AppColors.textMuted,
            ),
          ),
          const Spacer(),
          Text(
            value,
            style: TextStyle(
              fontWeight: FontWeight.w800,
              color: emphasize ? AppColors.sage : AppColors.text,
              fontSize: emphasize ? 18 : 15,
            ),
          ),
        ],
      ),
    );
  }
}

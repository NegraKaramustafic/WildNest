import 'package:ecommerce_mobile/models/ticket.dart';
import 'package:ecommerce_mobile/screens/tickets/ticket_summary_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

class BuyTicketScreen extends StatefulWidget {
  const BuyTicketScreen({super.key});

  @override
  State<BuyTicketScreen> createState() => _BuyTicketScreenState();
}

class _BuyTicketScreenState extends State<BuyTicketScreen> {
  static const _dayCount = 14;
  static const _maxVisitors = 20;

  late final List<DateTime> _dates;
  late DateTime _selectedDate;
  int _visitors = 1;

  @override
  void initState() {
    super.initState();
    final today = DateUtils.dateOnly(DateTime.now());
    _dates = List.generate(_dayCount, (index) => today.add(Duration(days: index)));
    _selectedDate = today;
  }

  double get _total => zooEntryPricePerPerson * _visitors;

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
                    'Buy a ticket',
                    style: GoogleFonts.playfairDisplay(
                      fontSize: 32,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 22),
                  const Row(
                    children: [
                      Icon(Icons.calendar_today_outlined, size: 18, color: AppColors.sage),
                      SizedBox(width: 8),
                      Text(
                        'Choose a date',
                        style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  SizedBox(
                    height: 84,
                    child: ListView.separated(
                      scrollDirection: Axis.horizontal,
                      itemCount: _dates.length,
                      separatorBuilder: (_, _) => const SizedBox(width: 10),
                      itemBuilder: (context, index) {
                        final date = _dates[index];
                        final selected = DateUtils.isSameDay(date, _selectedDate);
                        return _DateChip(
                          date: date,
                          selected: selected,
                          onTap: () => setState(() => _selectedDate = date),
                        );
                      },
                    ),
                  ),
                  const SizedBox(height: 28),
                  const Row(
                    children: [
                      Icon(Icons.group_outlined, size: 18, color: AppColors.sage),
                      SizedBox(width: 8),
                      Text(
                        'Number of visitors',
                        style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.fromLTRB(16, 14, 12, 14),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(22),
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                visitorLabel(_visitors),
                                style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                              ),
                              Text(
                                '${formatMoney(zooEntryPricePerPerson)} per person',
                                style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                              ),
                            ],
                          ),
                        ),
                        _StepperButton(
                          icon: Icons.remove,
                          onTap: _visitors > 1
                              ? () => setState(() => _visitors -= 1)
                              : null,
                        ),
                        Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 12),
                          child: Text(
                            '$_visitors',
                            style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
                          ),
                        ),
                        _StepperButton(
                          icon: Icons.add,
                          onTap: _visitors < _maxVisitors
                              ? () => setState(() => _visitors += 1)
                              : null,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 14),
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(22),
                    ),
                    child: Row(
                      children: [
                        const Text(
                          'Total price',
                          style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                        ),
                        const Spacer(),
                        Text(
                          formatMoney(_total),
                          style: const TextStyle(
                            color: AppColors.sage,
                            fontWeight: FontWeight.w800,
                            fontSize: 22,
                          ),
                        ),
                        const SizedBox(width: 10),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: AppColors.mint,
                            borderRadius: BorderRadius.circular(16),
                          ),
                          child: Text(
                            _visitors == 1 ? '1× ticket' : '$_visitors× tickets',
                            style: const TextStyle(
                              color: AppColors.sageDark,
                              fontWeight: FontWeight.w700,
                              fontSize: 12,
                            ),
                          ),
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
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => TicketSummaryScreen(
                        visitDate: _selectedDate,
                        visitorCount: _visitors,
                      ),
                    ),
                  );
                },
                child: const Text('Continue'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DateChip extends StatelessWidget {
  const _DateChip({
    required this.date,
    required this.selected,
    required this.onTap,
  });

  final DateTime date;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(18),
      child: Ink(
        width: 78,
        padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
        decoration: BoxDecoration(
          color: selected ? AppColors.sage : AppColors.surface,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(
            color: selected ? AppColors.sage : AppColors.border,
          ),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              formatDateChipTitle(date),
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color: selected ? Colors.white : AppColors.textMuted,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              formatDayMonth(date),
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w700,
                color: selected ? Colors.white : AppColors.text,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StepperButton extends StatelessWidget {
  const _StepperButton({required this.icon, this.onTap});

  final IconData icon;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Ink(
        width: 36,
        height: 36,
        decoration: BoxDecoration(
          color: AppColors.cream,
          borderRadius: BorderRadius.circular(16),
        ),
        child: Icon(
          icon,
          size: 18,
          color: onTap == null ? AppColors.navUnselected : AppColors.text,
        ),
      ),
    );
  }
}

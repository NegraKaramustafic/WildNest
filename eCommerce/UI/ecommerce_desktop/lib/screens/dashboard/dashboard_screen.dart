import 'package:ecommerce_desktop/models/dashboard.dart';
import 'package:ecommerce_desktop/providers/dashboard_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/admin_list.dart';
import 'package:ecommerce_desktop/widgets/review_sentiment_section.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  bool _loading = true;
  String? _error;
  DashboardSnapshot? _snapshot;
  int _sentimentReload = 0;

  @override
  void initState() {
    super.initState();
    _load(initial: true);
  }

  Future<void> _load({bool initial = false}) async {
    if (!initial) {
      setState(() {
        _loading = true;
        _error = null;
        _sentimentReload += 1;
      });
    }
    try {
      final snapshot = await context.read<DashboardProvider>().load();
      if (!mounted) return;
      setState(() {
        _snapshot = snapshot;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = friendlyError(error);
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(28, 28, 28, 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Dashboard', style: TextStyle(fontSize: 26, fontWeight: FontWeight.w700)),
                    SizedBox(height: 4),
                    Text(
                      'Reservations, visitors, revenue, adoptions, and review sentiment',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  ],
                ),
              ),
              Tooltip(
                message: 'Refresh',
                child: IconButton(
                  onPressed: _loading ? null : _load,
                  icon: const Icon(Icons.refresh),
                ),
              ),
            ],
          ),
          const SizedBox(height: 18),
          Expanded(child: _body()),
        ],
      ),
    );
  }

  Widget _body() {
    if (_loading && _snapshot == null) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null && _snapshot == null) {
      return ListError(message: _error!, onRetry: _load);
    }
    final snapshot = _snapshot;
    if (snapshot == null) {
      return const Center(child: CircularProgressIndicator());
    }
    return ListView(
      children: [
        if (_loading) const LinearProgressIndicator(minHeight: 2),
        _KpiGrid(snapshot: snapshot),
        const SizedBox(height: 16),
        LayoutBuilder(
          builder: (context, constraints) {
            final wide = constraints.maxWidth >= 900;
            final reservations = _ReservationsChart(months: snapshot.months);
            final revenue = _RevenueChart(months: snapshot.months);
            if (!wide) {
              return Column(children: [reservations, const SizedBox(height: 12), revenue]);
            }
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(child: reservations),
                const SizedBox(width: 12),
                Expanded(child: revenue),
              ],
            );
          },
        ),
        const SizedBox(height: 12),
        LayoutBuilder(
          builder: (context, constraints) {
            final wide = constraints.maxWidth >= 900;
            final activities = _TopActivitiesChart(activities: snapshot.topActivities);
            final donations = _DonationStatusChart(rows: snapshot.donationsByStatus);
            if (!wide) {
              return Column(children: [activities, const SizedBox(height: 12), donations]);
            }
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(child: activities),
                const SizedBox(width: 12),
                Expanded(child: donations),
              ],
            );
          },
        ),
        const SizedBox(height: 12),
        LayoutBuilder(
          builder: (context, constraints) {
            final wide = constraints.maxWidth >= 900;
            final visitors = _VisitorsChart(months: snapshot.visitorMonths);
            final trend = _RevenueTrendChart(months: snapshot.revenueMonths);
            if (!wide) {
              return Column(children: [visitors, const SizedBox(height: 12), trend]);
            }
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(child: visitors),
                const SizedBox(width: 12),
                Expanded(child: trend),
              ],
            );
          },
        ),
        const SizedBox(height: 12),
        LayoutBuilder(
          builder: (context, constraints) {
            final wide = constraints.maxWidth >= 900;
            final categories = _AdoptionCategoryChart(categories: snapshot.adoptionsByCategory);
            final recent = _RecentActivityCard(items: snapshot.recentActivity);
            if (!wide) {
              return Column(children: [categories, const SizedBox(height: 12), recent]);
            }
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(child: categories),
                const SizedBox(width: 12),
                Expanded(child: recent),
              ],
            );
          },
        ),
        const SizedBox(height: 16),
        ReviewSentimentPanel(reloadToken: _sentimentReload),
        const SizedBox(height: 12),
      ],
    );
  }
}

class _KpiGrid extends StatelessWidget {
  const _KpiGrid({required this.snapshot});

  final DashboardSnapshot snapshot;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = constraints.maxWidth >= 1100 ? 3 : 2;
        final width = (constraints.maxWidth - (columns - 1) * 12) / columns;
        final cards = [
          _KpiCard(
            width: width,
            title: 'Reservations today',
            value: formatCount(snapshot.reservationsToday),
            detail: 'Created today.',
          ),
          _KpiCard(
            width: width,
            title: 'Reservations this month',
            value: formatCount(snapshot.reservationsThisMonth),
            detail: 'Created this month.',
          ),
          _KpiCard(
            width: width,
            title: 'Revenue this month',
            value: formatMoney(snapshot.revenueThisMonth),
            detail:
                'Tickets ${formatMoney(snapshot.ticketRevenueThisMonth)} · Donations ${formatMoney(snapshot.donationRevenueThisMonth)}',
          ),
          _KpiCard(
            width: width,
            title: 'Active schedules',
            value: formatCount(snapshot.activeSchedules),
            detail: 'Schedules that are not cancelled.',
          ),
          _KpiCard(
            width: width,
            title: 'New users this month',
            value: formatCount(snapshot.newUsersThisMonth),
            detail: 'Accounts created this month.',
          ),
          _KpiCard(
            width: width,
            title: 'Average rating',
            value: formatRating(snapshot.averageRating),
            detail: snapshot.averageRating == null ? 'No reviews yet.' : 'Across all reviews.',
          ),
          _KpiCard(
            width: width,
            title: 'Total visitors',
            value: formatCount(snapshot.totalVisitors),
            detail: 'Completed ticket payments.',
          ),
          _KpiCard(
            width: width,
            title: 'Total animals',
            value: formatCount(snapshot.totalAnimals),
            detail: 'Animals in the zoo.',
          ),
          _KpiCard(
            width: width,
            title: 'Total reservations',
            value: formatCount(snapshot.totalReservations),
            detail: 'All reservations.',
          ),
          _KpiCard(
            width: width,
            title: 'Completed donations',
            value: formatCount(snapshot.completedDonationCount),
            detail: '${formatMoney(snapshot.completedDonationAmount)} completed.',
          ),
          _KpiCard(
            width: width,
            title: 'Total revenue',
            value: formatMoney(snapshot.totalRevenue),
            detailLines: 4,
            detail:
                'Tickets ${formatMoney(snapshot.ticketPaymentTotal)} · Reservations ${formatMoney(snapshot.reservationPaymentTotal)} · Adoptions ${formatMoney(snapshot.adoptionPaymentTotal)} · Donations ${formatMoney(snapshot.donationPaymentTotal)}',
          ),
          _KpiCard(
            width: width,
            title: 'Active adoptions',
            value: formatCount(snapshot.activeAdoptions),
            detail: 'Status is active.',
          ),
        ];
        return Wrap(spacing: 12, runSpacing: 12, children: cards);
      },
    );
  }
}

class _KpiCard extends StatelessWidget {
  const _KpiCard({
    required this.width,
    required this.title,
    required this.value,
    required this.detail,
    this.detailLines = 2,
  });

  final double width;
  final String title;
  final String value;
  final String detail;
  final int detailLines;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: width,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: AppColors.border),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(color: AppColors.textMuted, fontSize: 13, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 4),
            Text(
              value,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 2),
            Text(
              detail,
              maxLines: detailLines,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
            ),
          ],
        ),
      ),
    );
  }
}

class _ChartCard extends StatelessWidget {
  const _ChartCard({required this.title, required this.subtitle, required this.child});

  final String title;
  final String subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontWeight: FontWeight.w700)),
          const SizedBox(height: 2),
          Text(
            subtitle,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
          ),
          const SizedBox(height: 12),
          child,
        ],
      ),
    );
  }
}

class _EmptyChart extends StatelessWidget {
  const _EmptyChart({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 180,
      child: Center(
        child: Text(
          message,
          textAlign: TextAlign.center,
          style: const TextStyle(color: AppColors.textMuted),
        ),
      ),
    );
  }
}

class _ReservationsChart extends StatelessWidget {
  const _ReservationsChart({required this.months});

  final List<DashboardMonth> months;

  @override
  Widget build(BuildContext context) {
    final hasData = months.any((month) => month.reservationCount > 0);
    return _ChartCard(
      title: 'Reservations',
      subtitle: 'Created in the last 12 months.',
      child: !hasData
          ? const _EmptyChart(message: 'No reservations in the last 12 months.')
          : _MonthLineChart(
              months: months,
              color: AppColors.green,
              valueOf: (month) => month.reservationCount.toDouble(),
              tooltipOf: (month) =>
                  '${formatMonth(month.year, month.month)}\n${formatCount(month.reservationCount)} reservations',
            ),
    );
  }
}

class _RevenueChart extends StatelessWidget {
  const _RevenueChart({required this.months});

  final List<DashboardMonth> months;

  @override
  Widget build(BuildContext context) {
    final hasData = months.any((month) => month.revenue > 0);
    return _ChartCard(
      title: 'Revenue',
      subtitle: 'Completed ticket and donation payments.',
      child: !hasData
          ? const _EmptyChart(message: 'No ticket or donation revenue in the last 12 months.')
          : _MonthLineChart(
              months: months,
              color: const Color(0xFF2F6B4F),
              valueOf: (month) => month.revenue,
              tooltipOf: (month) => '${formatMonth(month.year, month.month)}\n${formatMoney(month.revenue)}',
              formatAxis: formatMoney,
              axisReservedSize: 92,
            ),
    );
  }
}

class _MonthLineChart extends StatelessWidget {
  const _MonthLineChart({
    required this.months,
    required this.color,
    required this.valueOf,
    required this.tooltipOf,
    this.formatAxis,
    this.axisReservedSize = 36,
  });

  final List<DashboardMonth> months;
  final Color color;
  final double Function(DashboardMonth month) valueOf;
  final String Function(DashboardMonth month) tooltipOf;
  final String Function(double value)? formatAxis;
  final double axisReservedSize;

  @override
  Widget build(BuildContext context) {
    var maxValue = 0.0;
    for (final month in months) {
      final value = valueOf(month);
      if (value > maxValue) maxValue = value;
    }
    final maxY = maxValue <= 0 ? 1.0 : (maxValue * 1.2).ceilToDouble();
    final interval = maxY <= 5 ? 1.0 : (maxY / 4).ceilToDouble();
    return SizedBox(
      height: 220,
      child: LineChart(
        LineChartData(
          minY: 0,
          maxY: maxY,
          gridData: const FlGridData(show: true, drawVerticalLine: false),
          borderData: FlBorderData(show: false),
          lineTouchData: LineTouchData(
            touchTooltipData: LineTouchTooltipData(
              getTooltipItems: (spots) {
                return [
                  for (final spot in spots)
                    if (spot.x.toInt() >= 0 && spot.x.toInt() < months.length)
                      LineTooltipItem(
                        tooltipOf(months[spot.x.toInt()]),
                        const TextStyle(color: Colors.white, fontSize: 12),
                      ),
                ];
              },
            ),
          ),
          titlesData: FlTitlesData(
            topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            leftTitles: AxisTitles(
              sideTitles: SideTitles(
                showTitles: true,
                reservedSize: axisReservedSize,
                interval: interval,
                getTitlesWidget: (value, meta) {
                  final label = formatAxis == null ? formatCount(value) : formatAxis!(value);
                  return Text(
                    label,
                    maxLines: 1,
                    softWrap: false,
                    style: const TextStyle(fontSize: 10, color: AppColors.textMuted),
                  );
                },
              ),
            ),
            bottomTitles: AxisTitles(
              sideTitles: SideTitles(
                showTitles: true,
                reservedSize: 28,
                getTitlesWidget: (value, meta) {
                  final index = value.toInt();
                  if (index < 0 || index >= months.length || value != index.toDouble()) {
                    return const SizedBox.shrink();
                  }
                  if (months.length > 6 && index % 2 != 0 && index != months.length - 1) {
                    return const SizedBox.shrink();
                  }
                  final point = months[index];
                  final month = formatMonth(point.year, point.month).split(' ').first;
                  final label = index == 0 || point.month == 1 ? '$month ${point.year % 100}' : month;
                  return Padding(
                    padding: const EdgeInsets.only(top: 6),
                    child: Text(label, style: const TextStyle(fontSize: 10, color: AppColors.textMuted)),
                  );
                },
              ),
            ),
          ),
          lineBarsData: [
            LineChartBarData(
              spots: [
                for (var index = 0; index < months.length; index++)
                  FlSpot(index.toDouble(), valueOf(months[index])),
              ],
              isCurved: true,
              color: color,
              barWidth: 3,
              dotData: const FlDotData(show: false),
              belowBarData: BarAreaData(show: true, color: color.withValues(alpha: 0.12)),
            ),
          ],
        ),
      ),
    );
  }
}

class _TopActivitiesChart extends StatelessWidget {
  const _TopActivitiesChart({required this.activities});

  final List<DashboardActivity> activities;

  @override
  Widget build(BuildContext context) {
    return _ChartCard(
      title: 'Top activities',
      subtitle: 'Reservations in the last 12 months, excluding cancelled.',
      child: activities.isEmpty
          ? const _EmptyChart(message: 'No activity reservations in the last 12 months.')
          : _ActivityBars(activities: activities),
    );
  }
}

class _ActivityBars extends StatelessWidget {
  const _ActivityBars({required this.activities});

  final List<DashboardActivity> activities;

  @override
  Widget build(BuildContext context) {
    var maxCount = 1;
    for (final activity in activities) {
      if (activity.reservationCount > maxCount) maxCount = activity.reservationCount;
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          height: 180,
          child: BarChart(
            BarChartData(
              maxY: maxCount.toDouble(),
              alignment: BarChartAlignment.spaceAround,
              gridData: const FlGridData(show: true, drawVerticalLine: false),
              borderData: FlBorderData(show: false),
              barTouchData: BarTouchData(
                touchTooltipData: BarTouchTooltipData(
                  getTooltipItem: (group, groupIndex, rod, rodIndex) {
                    final activity = activities[group.x];
                    return BarTooltipItem(
                      '${activity.activityName}\n${formatCount(activity.reservationCount)} · ${formatMoney(activity.revenue)}',
                      const TextStyle(color: Colors.white, fontSize: 12),
                    );
                  },
                ),
              ),
              titlesData: FlTitlesData(
                topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                leftTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 28,
                    getTitlesWidget: (value, meta) {
                      if (value != value.roundToDouble()) return const SizedBox.shrink();
                      return Text(
                        formatCount(value),
                        style: const TextStyle(fontSize: 10, color: AppColors.textMuted),
                      );
                    },
                  ),
                ),
                bottomTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 22,
                    getTitlesWidget: (value, meta) {
                      final index = value.toInt();
                      if (index < 0 || index >= activities.length) return const SizedBox.shrink();
                      return Text('${index + 1}', style: const TextStyle(fontSize: 11, color: AppColors.textMuted));
                    },
                  ),
                ),
              ),
              barGroups: [
                for (var index = 0; index < activities.length; index++)
                  BarChartGroupData(
                    x: index,
                    barRods: [
                      BarChartRodData(
                        toY: activities[index].reservationCount.toDouble(),
                        color: AppColors.green,
                        width: 18,
                        borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),
        for (var index = 0; index < activities.length; index++)
          Padding(
            padding: const EdgeInsets.only(bottom: 4),
            child: EllipsisText(
              '${index + 1}. ${activities[index].activityName} · ${formatCount(activities[index].reservationCount)} · ${formatMoney(activities[index].revenue)}',
              style: const TextStyle(fontSize: 12),
            ),
          ),
      ],
    );
  }
}

class _DonationStatusChart extends StatelessWidget {
  const _DonationStatusChart({required this.rows});

  final List<DashboardDonationStatus> rows;

  @override
  Widget build(BuildContext context) {
    return _ChartCard(
      title: 'Donations by status',
      subtitle: 'All donations, grouped by payment status.',
      child: rows.isEmpty ? const _EmptyChart(message: 'No donations yet.') : _DonationPie(rows: rows),
    );
  }
}

class _DonationPie extends StatelessWidget {
  const _DonationPie({required this.rows});

  final List<DashboardDonationStatus> rows;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 220,
      child: Row(
        children: [
          Expanded(
            child: PieChart(
              PieChartData(
                centerSpaceRadius: 36,
                sectionsSpace: 2,
                sections: [
                  for (final row in rows)
                    PieChartSectionData(
                      value: row.count.toDouble(),
                      color: _statusColor(row.status),
                      title: '',
                      radius: 46,
                    ),
                ],
              ),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                for (final row in rows)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        Container(
                          width: 10,
                          height: 10,
                          decoration: BoxDecoration(color: _statusColor(row.status), shape: BoxShape.circle),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: EllipsisText(
                            '${row.status} · ${formatCount(row.count)} · ${formatMoney(row.amount)}',
                            style: const TextStyle(fontSize: 12),
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _VisitorsChart extends StatelessWidget {
  const _VisitorsChart({required this.months});

  final List<DashboardVisitorMonth> months;

  @override
  Widget build(BuildContext context) {
    final hasData = months.any((month) => month.visitors > 0);
    return _ChartCard(
      title: 'Visitors',
      subtitle: 'Completed ticket visits in the last 12 months.',
      child: !hasData
          ? const _EmptyChart(message: 'No completed ticket visits in the last 12 months.')
          : _VisitorLine(months: months),
    );
  }
}

class _VisitorLine extends StatelessWidget {
  const _VisitorLine({required this.months});

  final List<DashboardVisitorMonth> months;

  @override
  Widget build(BuildContext context) {
    var maxValue = 0;
    for (final month in months) {
      if (month.visitors > maxValue) maxValue = month.visitors;
    }
    final maxY = maxValue <= 0 ? 1.0 : (maxValue * 1.2).ceilToDouble();
    final interval = maxY <= 5 ? 1.0 : (maxY / 4).ceilToDouble();
    return SizedBox(
      height: 220,
      child: LineChart(
        LineChartData(
          minY: 0,
          maxY: maxY,
          gridData: const FlGridData(show: true, drawVerticalLine: false),
          borderData: FlBorderData(show: false),
          lineTouchData: LineTouchData(
            touchTooltipData: LineTouchTooltipData(
              getTooltipItems: (spots) {
                return [
                  for (final spot in spots)
                    if (spot.x.toInt() >= 0 && spot.x.toInt() < months.length)
                      LineTooltipItem(
                        '${formatMonth(months[spot.x.toInt()].year, months[spot.x.toInt()].month)}\n${formatCount(months[spot.x.toInt()].visitors)} visitors',
                        const TextStyle(color: Colors.white, fontSize: 12),
                      ),
                ];
              },
            ),
          ),
          titlesData: _monthTitles(
            count: months.length,
            interval: interval,
            labelAt: (index) {
              final point = months[index];
              final month = formatMonth(point.year, point.month).split(' ').first;
              return index == 0 || point.month == 1 ? '$month ${point.year % 100}' : month;
            },
            formatAxis: formatCount,
          ),
          lineBarsData: [
            LineChartBarData(
              spots: [
                for (var index = 0; index < months.length; index++) FlSpot(index.toDouble(), months[index].visitors.toDouble()),
              ],
              isCurved: true,
              color: AppColors.green,
              barWidth: 3,
              dotData: const FlDotData(show: false),
              belowBarData: BarAreaData(show: true, color: AppColors.green.withValues(alpha: 0.12)),
            ),
          ],
        ),
      ),
    );
  }
}

class _RevenueTrendChart extends StatelessWidget {
  const _RevenueTrendChart({required this.months});

  final List<DashboardRevenueMonth> months;

  @override
  Widget build(BuildContext context) {
    final hasData = months.any((month) => month.total > 0);
    return _ChartCard(
      title: 'Revenue trend',
      subtitle: 'Completed ticket, reservation, adoption, and donation payments.',
      child: !hasData
          ? const _EmptyChart(message: 'No completed payments in the last 12 months.')
          : _RevenueBars(months: months),
    );
  }
}

class _RevenueBars extends StatelessWidget {
  const _RevenueBars({required this.months});

  final List<DashboardRevenueMonth> months;

  static const _ticket = AppColors.green;
  static const _reservation = Color(0xFF2F6B4F);
  static const _adoption = Color(0xFFC47B17);
  static const _donation = Color(0xFF3D6B8C);

  @override
  Widget build(BuildContext context) {
    var maxValue = 0.0;
    for (final month in months) {
      if (month.total > maxValue) maxValue = month.total;
    }
    final maxY = maxValue <= 0 ? 1.0 : (maxValue * 1.2).ceilToDouble();
    final interval = maxY <= 5 ? 1.0 : (maxY / 4).ceilToDouble();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          height: 220,
          child: BarChart(
            BarChartData(
              maxY: maxY,
              alignment: BarChartAlignment.spaceAround,
              gridData: const FlGridData(show: true, drawVerticalLine: false),
              borderData: FlBorderData(show: false),
              barTouchData: BarTouchData(
                touchTooltipData: BarTouchTooltipData(
                  getTooltipItem: (group, groupIndex, rod, rodIndex) {
                    final month = months[group.x];
                    return BarTooltipItem(
                      '${formatMonth(month.year, month.month)}\nTickets ${formatMoney(month.ticketRevenue)}\nReservations ${formatMoney(month.reservationRevenue)}\nAdoptions ${formatMoney(month.adoptionRevenue)}\nDonations ${formatMoney(month.donationRevenue)}',
                      const TextStyle(color: Colors.white, fontSize: 12),
                    );
                  },
                ),
              ),
              titlesData: _monthTitles(
                count: months.length,
                interval: interval,
                labelAt: (index) {
                  final point = months[index];
                  final month = formatMonth(point.year, point.month).split(' ').first;
                  return index == 0 || point.month == 1 ? '$month ${point.year % 100}' : month;
                },
                formatAxis: formatMoney,
                leftReservedSize: 92,
              ),
              barGroups: [
                for (var index = 0; index < months.length; index++)
                  BarChartGroupData(
                    x: index,
                    barRods: [
                      BarChartRodData(
                        toY: months[index].total,
                        width: 14,
                        borderRadius: const BorderRadius.vertical(top: Radius.circular(3)),
                        rodStackItems: _stack(months[index]),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 12,
          runSpacing: 4,
          children: const [
            _LegendDot(color: _ticket, label: 'Tickets'),
            _LegendDot(color: _reservation, label: 'Reservations'),
            _LegendDot(color: _adoption, label: 'Adoptions'),
            _LegendDot(color: _donation, label: 'Donations'),
          ],
        ),
      ],
    );
  }

  List<BarChartRodStackItem> _stack(DashboardRevenueMonth month) {
    final items = <BarChartRodStackItem>[];
    var cursor = 0.0;
    void add(double amount, Color color) {
      if (amount <= 0) return;
      items.add(BarChartRodStackItem(cursor, cursor + amount, color));
      cursor += amount;
    }

    add(month.ticketRevenue, _ticket);
    add(month.reservationRevenue, _reservation);
    add(month.adoptionRevenue, _adoption);
    add(month.donationRevenue, _donation);
    return items;
  }
}

class _LegendDot extends StatelessWidget {
  const _LegendDot({required this.color, required this.label});

  final Color color;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(width: 10, height: 10, decoration: BoxDecoration(color: color, shape: BoxShape.circle)),
        const SizedBox(width: 6),
        Text(label, style: const TextStyle(fontSize: 12, color: AppColors.textMuted)),
      ],
    );
  }
}

class _AdoptionCategoryChart extends StatelessWidget {
  const _AdoptionCategoryChart({required this.categories});

  final List<DashboardCategoryCount> categories;

  @override
  Widget build(BuildContext context) {
    return _ChartCard(
      title: 'Adoptions by category',
      subtitle: 'Active adoptions.',
      child: categories.isEmpty ? const _EmptyChart(message: 'No active adoptions.') : _CategoryBars(categories: categories),
    );
  }
}

class _CategoryBars extends StatelessWidget {
  const _CategoryBars({required this.categories});

  final List<DashboardCategoryCount> categories;

  @override
  Widget build(BuildContext context) {
    var maxCount = 1;
    for (final category in categories) {
      if (category.count > maxCount) maxCount = category.count;
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          height: 180,
          child: BarChart(
            BarChartData(
              maxY: maxCount.toDouble(),
              alignment: BarChartAlignment.spaceAround,
              gridData: const FlGridData(show: true, drawVerticalLine: false),
              borderData: FlBorderData(show: false),
              barTouchData: BarTouchData(
                touchTooltipData: BarTouchTooltipData(
                  getTooltipItem: (group, groupIndex, rod, rodIndex) {
                    final category = categories[group.x];
                    return BarTooltipItem(
                      '${category.categoryName}\n${formatCount(category.count)} active',
                      const TextStyle(color: Colors.white, fontSize: 12),
                    );
                  },
                ),
              ),
              titlesData: FlTitlesData(
                topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                leftTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 28,
                    getTitlesWidget: (value, meta) {
                      if (value != value.roundToDouble()) return const SizedBox.shrink();
                      return Text(formatCount(value), style: const TextStyle(fontSize: 10, color: AppColors.textMuted));
                    },
                  ),
                ),
                bottomTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 22,
                    getTitlesWidget: (value, meta) {
                      final index = value.toInt();
                      if (index < 0 || index >= categories.length) return const SizedBox.shrink();
                      return Text('${index + 1}', style: const TextStyle(fontSize: 11, color: AppColors.textMuted));
                    },
                  ),
                ),
              ),
              barGroups: [
                for (var index = 0; index < categories.length; index++)
                  BarChartGroupData(
                    x: index,
                    barRods: [
                      BarChartRodData(
                        toY: categories[index].count.toDouble(),
                        color: const Color(0xFFC47B17),
                        width: 18,
                        borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),
        for (var index = 0; index < categories.length; index++)
          Padding(
            padding: const EdgeInsets.only(bottom: 4),
            child: EllipsisText(
              '${index + 1}. ${categories[index].categoryName} · ${formatCount(categories[index].count)}',
              style: const TextStyle(fontSize: 12),
            ),
          ),
      ],
    );
  }
}

class _RecentActivityCard extends StatelessWidget {
  const _RecentActivityCard({required this.items});

  final List<DashboardActivityItem> items;

  @override
  Widget build(BuildContext context) {
    return _ChartCard(
      title: 'Recent activity',
      subtitle: 'Latest reservations, donations, adoptions, and ticket purchases.',
      child: items.isEmpty
          ? const _EmptyChart(message: 'No recent activity.')
          : Column(
              children: [
                for (final item in items)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: _ActivityRow(item: item),
                  ),
              ],
            ),
    );
  }
}

class _ActivityRow extends StatelessWidget {
  const _ActivityRow({required this.item});

  final DashboardActivityItem item;

  @override
  Widget build(BuildContext context) {
    final amount = formatMoney(item.amount);
    final when = _formatUtc(item.occurredAtUtc);
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 520) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              EllipsisText(item.label, style: const TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 2),
              EllipsisText(
                '${item.type} · $amount · $when',
                style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
              ),
            ],
          );
        }
        return Row(
          children: [
            SizedBox(
              width: 92,
              child: Text(
                item.type,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontSize: 12, color: AppColors.textMuted, fontWeight: FontWeight.w600),
              ),
            ),
            Expanded(child: EllipsisText(item.label)),
            const SizedBox(width: 8),
            SizedBox(
              width: 96,
              child: Text(amount, maxLines: 1, overflow: TextOverflow.ellipsis, textAlign: TextAlign.end),
            ),
            const SizedBox(width: 8),
            SizedBox(
              width: 168,
              child: Text(
                when,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                textAlign: TextAlign.end,
                style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
              ),
            ),
          ],
        );
      },
    );
  }
}

String _formatUtc(DateTime value) {
  final utc = value.isUtc ? value : DateTime.utc(value.year, value.month, value.day, value.hour, value.minute);
  final clock = formatClock(Duration(hours: utc.hour, minutes: utc.minute));
  return '${formatDate(utc)}, $clock UTC';
}

FlTitlesData _monthTitles({
  required int count,
  required double interval,
  required String Function(int index) labelAt,
  required String Function(double value) formatAxis,
  double leftReservedSize = 36,
}) {
  return FlTitlesData(
    topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
    rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
    leftTitles: AxisTitles(
      sideTitles: SideTitles(
        showTitles: true,
        reservedSize: leftReservedSize,
        interval: interval,
        getTitlesWidget: (value, meta) {
          return Text(
            formatAxis(value),
            maxLines: 1,
            softWrap: false,
            style: const TextStyle(fontSize: 10, color: AppColors.textMuted),
          );
        },
      ),
    ),
    bottomTitles: AxisTitles(
      sideTitles: SideTitles(
        showTitles: true,
        reservedSize: 28,
        getTitlesWidget: (value, meta) {
          final index = value.toInt();
          if (index < 0 || index >= count || value != index.toDouble()) {
            return const SizedBox.shrink();
          }
          if (count > 6 && index % 2 != 0 && index != count - 1) {
            return const SizedBox.shrink();
          }
          return Padding(
            padding: const EdgeInsets.only(top: 6),
            child: Text(labelAt(index), style: const TextStyle(fontSize: 10, color: AppColors.textMuted)),
          );
        },
      ),
    ),
  );
}

Color _statusColor(String status) {
  switch (status) {
    case 'Completed':
      return AppColors.green;
    case 'Pending':
      return const Color(0xFFC47B17);
    case 'Failed':
      return AppColors.danger;
    case 'Refunded':
      return const Color(0xFF3D6B8C);
    default:
      return AppColors.textMuted;
  }
}

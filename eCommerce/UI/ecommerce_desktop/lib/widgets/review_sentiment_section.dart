import 'package:ecommerce_desktop/models/review_sentiment_statistics.dart';
import 'package:ecommerce_desktop/providers/review_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/sentiment_badge.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

/// Loads review sentiment statistics. Drop this on the reviews screen or the dashboard.
class ReviewSentimentPanel extends StatefulWidget {
  const ReviewSentimentPanel({super.key, this.reloadToken = 0});

  final int reloadToken;

  @override
  State<ReviewSentimentPanel> createState() => _ReviewSentimentPanelState();
}

class _ReviewSentimentPanelState extends State<ReviewSentimentPanel> {
  bool _loading = true;
  String? _error;
  ReviewSentimentStatistics? _statistics;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(covariant ReviewSentimentPanel oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.reloadToken != widget.reloadToken) {
      _load();
    }
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final statistics = await context.read<ReviewProvider>().sentimentStatistics();
      if (!mounted) return;
      setState(() {
        _statistics = statistics;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.toString().replaceFirst('Bad state: ', '');
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return ReviewSentimentSection(
      loading: _loading,
      error: _error,
      statistics: _statistics,
      onRetry: _load,
    );
  }
}

class ReviewSentimentSection extends StatelessWidget {
  const ReviewSentimentSection({
    super.key,
    required this.loading,
    this.error,
    this.statistics,
    this.onRetry,
  });

  final bool loading;
  final String? error;
  final ReviewSentimentStatistics? statistics;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Review sentiment',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 4),
        const Text(
          'Each review text is classified as positive or negative.',
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(color: AppColors.textMuted),
        ),
        const SizedBox(height: 12),
        if (loading)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 28),
            child: Center(child: CircularProgressIndicator()),
          )
        else if (error != null)
          _MessageCard(
            message: error!,
            action: onRetry == null
                ? null
                : OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
          )
        else if (statistics == null || statistics!.labeledReviews == 0)
          _MessageCard(
            message: statistics != null && statistics!.totalReviews > 0
                ? 'No labeled reviews yet.'
                : 'No reviews yet.',
          )
        else
          _SentimentBody(statistics: statistics!),
      ],
    );
  }
}

class _SentimentBody extends StatelessWidget {
  const _SentimentBody({required this.statistics});

  final ReviewSentimentStatistics statistics;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = constraints.maxWidth >= 900 ? 3 : 2;
        final cardWidth = (constraints.maxWidth - (columns - 1) * 12) / columns;
        final wideCharts = constraints.maxWidth >= 760;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Wrap(
              spacing: 12,
              runSpacing: 12,
              children: [
                _SummaryCard(
                  width: cardWidth,
                  title: 'Reviews',
                  value: formatCount(statistics.totalReviews),
                  detail: 'Average rating ${formatRating(statistics.overallAverageRating)}',
                ),
                for (final name in const ['Positive', 'Negative'])
                  _SummaryCard(
                    width: cardWidth,
                    title: name,
                    value: formatCount(statistics.label(name)?.count ?? 0),
                    detail:
                        '${formatPercent(statistics.label(name)?.percentage ?? 0)} · avg ${formatRating(statistics.label(name)?.averageRating)}',
                    accent: sentimentColors(name),
                  ),
              ],
            ),
            if (statistics.unlabeledReviews > 0) ...[
              const SizedBox(height: 8),
              Text(
                '${formatCount(statistics.unlabeledReviews)} reviews are not labeled yet.',
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
              ),
            ],
            const SizedBox(height: 12),
            if (wideCharts)
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(flex: 2, child: _DistributionCard(statistics: statistics)),
                  const SizedBox(width: 12),
                  Expanded(flex: 3, child: _TrendCard(statistics: statistics)),
                ],
              )
            else ...[
              _DistributionCard(statistics: statistics),
              const SizedBox(height: 12),
              _TrendCard(statistics: statistics),
            ],
            if (statistics.activities.isNotEmpty) ...[
              const SizedBox(height: 12),
              _BreakdownCard(
                title: 'By activity',
                rows: [
                  for (final activity in statistics.activities)
                    _BreakdownRow(
                      title: activity.activityName,
                      detail:
                          'Positive ${formatPercent(activity.positivePercentage)} · Negative ${formatPercent(activity.negativePercentage)} · avg ${formatRating(activity.averageRating)}',
                    ),
                ],
              ),
            ],
            if (statistics.animals.isNotEmpty) ...[
              const SizedBox(height: 12),
              _BreakdownCard(
                title: 'By animal',
                rows: [
                  for (final animal in statistics.animals)
                    _BreakdownRow(
                      title: animal.animalName,
                      detail:
                          'Positive ${formatPercent(animal.positivePercentage)} · Negative ${formatPercent(animal.negativePercentage)}',
                    ),
                ],
              ),
            ],
          ],
        );
      },
    );
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({
    required this.width,
    required this.title,
    required this.value,
    required this.detail,
    this.accent,
  });

  final double width;
  final String title;
  final String value;
  final String detail;
  final SentimentColors? accent;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: width,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: accent?.border ?? AppColors.border),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: accent?.foreground ?? AppColors.textMuted,
                fontSize: 13,
                fontWeight: FontWeight.w600,
              ),
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
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
            ),
          ],
        ),
      ),
    );
  }
}

class _DistributionCard extends StatelessWidget {
  const _DistributionCard({required this.statistics});

  final ReviewSentimentStatistics statistics;

  @override
  Widget build(BuildContext context) {
    final sections = <PieChartSectionData>[];
    for (final name in const ['Positive', 'Negative']) {
      final count = statistics.label(name)?.count ?? 0;
      if (count <= 0) continue;
      sections.add(
        PieChartSectionData(
          value: count.toDouble(),
          color: sentimentColors(name).chart,
          title: '',
          radius: 46,
        ),
      );
    }

    return _ChartCard(
      title: 'Distribution',
      child: SizedBox(
        height: 180,
        child: Row(
          children: [
            Expanded(
              child: PieChart(
                PieChartData(
                  sections: sections,
                  centerSpaceRadius: 36,
                  sectionsSpace: 2,
                ),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  for (final name in const ['Positive', 'Negative'])
                    _LegendLine(
                      label: name,
                      detail:
                          '${formatCount(statistics.label(name)?.count ?? 0)} · ${formatPercent(statistics.label(name)?.percentage ?? 0)}',
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _LegendLine extends StatelessWidget {
  const _LegendLine({required this.label, required this.detail});

  final String label;
  final String detail;

  @override
  Widget build(BuildContext context) {
    final colors = sentimentColors(label);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Container(
            width: 10,
            height: 10,
            decoration: BoxDecoration(color: colors.chart, shape: BoxShape.circle),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              '$label $detail',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 13),
            ),
          ),
        ],
      ),
    );
  }
}

class _TrendCard extends StatelessWidget {
  const _TrendCard({required this.statistics});

  final ReviewSentimentStatistics statistics;

  @override
  Widget build(BuildContext context) {
    final points = statistics.monthlyTrend;
    if (points.isEmpty) {
      return const _ChartCard(
        title: 'Monthly trend',
        child: SizedBox(
          height: 180,
          child: Center(
            child: Text(
              'No monthly trend yet.',
              style: TextStyle(color: AppColors.textMuted),
            ),
          ),
        ),
      );
    }

    var maxCount = 1;
    for (final point in points) {
      final local = point.positiveCount > point.negativeCount
          ? point.positiveCount
          : point.negativeCount;
      if (local > maxCount) maxCount = local;
    }

    return _ChartCard(
      title: 'Monthly trend',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Wrap(
            spacing: 12,
            runSpacing: 4,
            children: [
              _InlineLegend(label: 'Positive'),
              _InlineLegend(label: 'Negative'),
            ],
          ),
          const SizedBox(height: 8),
          SizedBox(
            height: 180,
            child: BarChart(
              BarChartData(
                maxY: maxCount.toDouble(),
                alignment: BarChartAlignment.spaceAround,
                gridData: const FlGridData(show: true, drawVerticalLine: false),
                borderData: FlBorderData(show: false),
                titlesData: FlTitlesData(
                  topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                  rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                  leftTitles: AxisTitles(
                    sideTitles: SideTitles(
                      showTitles: true,
                      reservedSize: 28,
                      getTitlesWidget: (value, meta) {
                        if (value != value.roundToDouble()) {
                          return const SizedBox.shrink();
                        }
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
                      reservedSize: 28,
                      getTitlesWidget: (value, meta) {
                        final index = value.toInt();
                        if (index < 0 || index >= points.length) {
                          return const SizedBox.shrink();
                        }
                        final point = points[index];
                        return Padding(
                          padding: const EdgeInsets.only(top: 6),
                          child: Text(
                            formatMonth(point.year, point.month),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(fontSize: 10, color: AppColors.textMuted),
                          ),
                        );
                      },
                    ),
                  ),
                ),
                barGroups: [
                  for (var index = 0; index < points.length; index++)
                    BarChartGroupData(
                      x: index,
                      barsSpace: 3,
                      barRods: [
                        _rod('Positive', points[index].positiveCount),
                        _rod('Negative', points[index].negativeCount),
                      ],
                    ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  BarChartRodData _rod(String label, int count) {
    return BarChartRodData(
      toY: count.toDouble(),
      color: sentimentColors(label).chart,
      width: 8,
      borderRadius: const BorderRadius.vertical(top: Radius.circular(3)),
    );
  }
}

class _InlineLegend extends StatelessWidget {
  const _InlineLegend({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(
            color: sentimentColors(label).chart,
            shape: BoxShape.circle,
          ),
        ),
        const SizedBox(width: 6),
        Text(label, style: const TextStyle(fontSize: 12)),
      ],
    );
  }
}

class _BreakdownCard extends StatelessWidget {
  const _BreakdownCard({required this.title, required this.rows});

  final String title;
  final List<Widget> rows;

  @override
  Widget build(BuildContext context) {
    return _ChartCard(
      title: title,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxHeight: 140),
        child: ListView.separated(
          shrinkWrap: true,
          itemCount: rows.length,
          separatorBuilder: (_, _) => const Divider(height: 1),
          itemBuilder: (context, index) => rows[index],
        ),
      ),
    );
  }
}

class _BreakdownRow extends StatelessWidget {
  const _BreakdownRow({required this.title, required this.detail});

  final String title;
  final String detail;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, maxLines: 1, overflow: TextOverflow.ellipsis),
          const SizedBox(height: 2),
          Text(
            detail,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
          ),
        ],
      ),
    );
  }
}

class _ChartCard extends StatelessWidget {
  const _ChartCard({required this.title, required this.child});

  final String title;
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
          Text(
            title,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 8),
          child,
        ],
      ),
    );
  }
}

class _MessageCard extends StatelessWidget {
  const _MessageCard({required this.message, this.action});

  final String message;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 18),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(message, style: const TextStyle(color: AppColors.textMuted)),
          if (action != null) ...[
            const SizedBox(height: 12),
            action!,
          ],
        ],
      ),
    );
  }
}

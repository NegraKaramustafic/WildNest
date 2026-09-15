import 'package:ecommerce_mobile/models/activity.dart';
import 'package:ecommerce_mobile/models/activity_schedule.dart';
import 'package:ecommerce_mobile/models/review.dart';
import 'package:ecommerce_mobile/providers/activity_provider.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/providers/review_provider.dart';
import 'package:ecommerce_mobile/providers/schedule_provider.dart';
import 'package:ecommerce_mobile/screens/activities/confirm_reservation_screen.dart';
import 'package:ecommerce_mobile/screens/activities/leave_review_dialog.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/formatters.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/activity_visuals.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class ActivityDetailScreen extends StatefulWidget {
  const ActivityDetailScreen({super.key, required this.activityId});

  final int activityId;

  @override
  State<ActivityDetailScreen> createState() => _ActivityDetailScreenState();
}

class _ActivityDetailScreenState extends State<ActivityDetailScreen> {
  Activity? _activity;
  List<ActivitySchedule> _schedules = [];
  List<Review> _reviews = [];
  ActivitySchedule? _selected;
  bool _hasReviewed = false;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final activityProvider = context.read<ActivityProvider>();
      final scheduleProvider = context.read<ScheduleProvider>();
      final reviewProvider = context.read<ReviewProvider>();
      final userId = context.read<AuthProvider>().currentUserId;
      final activity = await activityProvider.getById(widget.activityId);

      List<ActivitySchedule> schedules = [];
      try {
        schedules = await scheduleProvider.getForActivity(widget.activityId);
      } catch (_) {
        schedules = [];
      }

      List<Review> reviews = [];
      try {
        reviews = await reviewProvider.getForActivity(widget.activityId);
      } catch (_) {
        reviews = [];
      }

      var hasReviewed = false;
      if (userId != null) {
        hasReviewed = reviews.any((r) => r.userId == userId);
        if (!hasReviewed) {
          try {
            final mine = await reviewProvider.getMineForActivity(
              widget.activityId,
              userId,
            );
            hasReviewed = mine != null;
          } catch (_) {}
        }
      }

      if (!mounted) return;
      final bookable = schedules.where((s) => s.isBookable).toList();
      setState(() {
        _activity = activity;
        _schedules = bookable;
        _reviews = reviews;
        _hasReviewed = hasReviewed;
        _selected = _selected != null
            ? bookable.where((s) => s.id == _selected!.id).firstOrNull
            : (bookable.isEmpty ? null : bookable.first);
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = e.toString();
      });
      showAppError(context, e);
    }
  }

  int get _spotsLeft =>
      _selected?.capacityLeft ??
      (_schedules.isEmpty
          ? (_activity?.capacity ?? 0)
          : _schedules
                .map((s) => s.capacityLeft)
                .reduce((a, b) => a > b ? a : b));

  Future<void> _openReview() async {
    if (_hasReviewed) {
      alertBox(
        context,
        'Already reviewed',
        'You have already reviewed this activity.',
      );
      return;
    }
    final created = await showLeaveReviewDialog(
      context,
      activityId: widget.activityId,
    );
    if (created == true && mounted) {
      await _load();
    }
  }

  void _reserve() {
    final activity = _activity;
    final schedule = _selected;
    if (activity == null) return;
    if (schedule == null) {
      alertBox(
        context,
        'Pick a time',
        'Select an available time slot before reserving.',
      );
      return;
    }
    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => ConfirmReservationScreen(
          activity: activity,
          schedule: schedule,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: _loading
            ? const Center(
                child: CircularProgressIndicator(color: AppColors.sage),
              )
            : _error != null && _activity == null
            ? _errorView()
            : _content(_activity!),
      ),
    );
  }

  Widget _errorView() {
    return Column(
      children: [
        WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
        Expanded(
          child: Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(_error ?? 'Could not load this activity.'),
                  const SizedBox(height: 12),
                  ElevatedButton(onPressed: _load, child: const Text('Try again')),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _content(Activity activity) {
    return Column(
      children: [
        WildNestHeader(showBack: true, onBack: () => Navigator.pop(context)),
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.only(bottom: 32),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 4, 16, 0),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(28),
                    child: Container(
                      height: 200,
                      width: double.infinity,
                      color: AppColors.mint,
                      child: activity.imageUrl != null &&
                              activity.imageUrl!.isNotEmpty
                          ? Image.network(
                              activity.imageUrl!,
                              fit: BoxFit.cover,
                              errorBuilder: (_, _, _) => Center(
                                child: Text(
                                  activityEmoji(activity),
                                  style: const TextStyle(fontSize: 96),
                                ),
                              ),
                            )
                          : Center(
                              child: Text(
                                activityEmoji(activity),
                                style: const TextStyle(fontSize: 96),
                              ),
                            ),
                    ),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.fromLTRB(22, 22, 22, 0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        activity.name,
                        style: GoogleFonts.playfairDisplay(
                          fontSize: 30,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      if (activity.description != null &&
                          activity.description!.isNotEmpty) ...[
                        const SizedBox(height: 6),
                        Text(
                          activity.description!,
                          style: const TextStyle(
                            color: AppColors.textMuted,
                            fontSize: 15,
                            height: 1.4,
                          ),
                        ),
                      ],
                      const SizedBox(height: 14),
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: [
                          _InfoChip(
                            icon: Icons.schedule,
                            label: formatDurationHours(activity.duration),
                          ),
                          _InfoChip(
                            icon: Icons.group_outlined,
                            label: '$_spotsLeft spots left',
                          ),
                        ],
                      ),
                      const SizedBox(height: 22),
                      Text(
                        'Select a Time',
                        style: GoogleFonts.dmSans(
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 12),
                      if (_schedules.isEmpty)
                        const Text(
                          'No upcoming time slots are available.',
                          style: TextStyle(color: AppColors.textMuted),
                        )
                      else
                        _TimeSlotGrid(
                          schedules: _schedules,
                          selectedId: _selected?.id,
                          onSelected: (slot) => setState(() => _selected = slot),
                        ),
                      const SizedBox(height: 22),
                      Row(
                        children: [
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                formatMoney(activity.price),
                                style: const TextStyle(
                                  fontSize: 28,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.sage,
                                ),
                              ),
                              const Text(
                                'per person',
                                style: TextStyle(
                                  color: AppColors.textMuted,
                                  fontSize: 13,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: ElevatedButton(
                              onPressed: _schedules.isEmpty ? null : _reserve,
                              child: const Text('Reserve Now'),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 28),
                      const Divider(color: AppColors.border),
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          Text(
                            'Reviews',
                            style: GoogleFonts.playfairDisplay(
                              fontSize: 24,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const Spacer(),
                          Text(
                            _reviews.length == 1
                                ? '1 review'
                                : '${_reviews.length} reviews',
                            style: const TextStyle(
                              color: AppColors.textMuted,
                              fontSize: 13,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      if (_reviews.isEmpty)
                        const Padding(
                          padding: EdgeInsets.symmetric(vertical: 12),
                          child: Text(
                            'No reviews yet. Be the first to share your experience.',
                            style: TextStyle(color: AppColors.textMuted),
                          ),
                        )
                      else
                        ..._reviews.map(
                          (review) => Padding(
                            padding: const EdgeInsets.only(bottom: 10),
                            child: _ReviewTile(review: review),
                          ),
                        ),
                      if (!_hasReviewed) ...[
                        const SizedBox(height: 8),
                        SizedBox(
                          width: double.infinity,
                          child: ElevatedButton(
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppColors.sageDark,
                            ),
                            onPressed: _openReview,
                            child: const Text('Leave a review'),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _TimeSlotGrid extends StatelessWidget {
  const _TimeSlotGrid({
    required this.schedules,
    required this.selectedId,
    required this.onSelected,
  });

  final List<ActivitySchedule> schedules;
  final int? selectedId;
  final ValueChanged<ActivitySchedule> onSelected;

  @override
  Widget build(BuildContext context) {
    final dates = <DateTime>[];
    for (final slot in schedules) {
      final day = DateTime(slot.date.year, slot.date.month, slot.date.day);
      if (!dates.any((d) => d == day)) {
        dates.add(day);
      }
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (dates.length > 1) ...[
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: dates.map((day) {
              final selected = schedules
                  .where((s) => s.id == selectedId)
                  .firstOrNull;
              final isSelectedDay =
                  selected != null && DateUtils.isSameDay(selected.date, day);
              return GestureDetector(
                onTap: () {
                  final first = schedules.firstWhere(
                    (s) => DateUtils.isSameDay(s.date, day),
                  );
                  onSelected(first);
                },
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 8,
                  ),
                  decoration: BoxDecoration(
                    color: isSelectedDay ? AppColors.mint : AppColors.chip,
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(
                      color: isSelectedDay ? AppColors.sage : AppColors.border,
                    ),
                  ),
                  child: Text(
                    formatFriendlyDate(day),
                    style: TextStyle(
                      fontWeight: FontWeight.w600,
                      fontSize: 13,
                      color: isSelectedDay
                          ? AppColors.sageDark
                          : AppColors.text,
                    ),
                  ),
                ),
              );
            }).toList(),
          ),
          const SizedBox(height: 12),
        ],
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: [
            for (final slot in schedules)
              if (selectedId == null ||
                  dates.length <= 1 ||
                  DateUtils.isSameDay(
                    slot.date,
                    schedules
                            .where((s) => s.id == selectedId)
                            .firstOrNull
                            ?.date ??
                        slot.date,
                  ))
                _TimeChip(
                  label: formatClock(slot.startTime),
                  selected: slot.id == selectedId,
                  onTap: () => onSelected(slot),
                ),
          ],
        ),
      ],
    );
  }
}

class _TimeChip extends StatelessWidget {
  const _TimeChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: selected ? AppColors.mint : AppColors.chip,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: selected ? AppColors.sage : Colors.transparent,
          ),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontWeight: FontWeight.w600,
            color: selected ? AppColors.sageDark : AppColors.text,
          ),
        ),
      ),
    );
  }
}

class _InfoChip extends StatelessWidget {
  const _InfoChip({required this.label, this.icon});

  final String label;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
      decoration: BoxDecoration(
        color: AppColors.chip,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 14, color: AppColors.sage),
            const SizedBox(width: 4),
          ],
          Text(
            label,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppColors.sageDark,
            ),
          ),
        ],
      ),
    );
  }
}

class _ReviewTile extends StatelessWidget {
  const _ReviewTile({required this.review});

  final Review review;

  @override
  Widget build(BuildContext context) {
    final name = abbreviatedReviewerName(review.userFullName);
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 18,
                backgroundColor: AppColors.mint,
                child: Text(
                  initialsFor(review.userFullName),
                  style: const TextStyle(
                    color: AppColors.sageDark,
                    fontWeight: FontWeight.w700,
                    fontSize: 13,
                  ),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      name,
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    if (review.createdAt != null)
                      Text(
                        formatReviewDate(review.createdAt!),
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.textMuted,
                        ),
                      ),
                  ],
                ),
              ),
              Row(
                children: List.generate(5, (index) {
                  final filled = index < review.rating;
                  return Icon(
                    filled ? Icons.star_rounded : Icons.star_border_rounded,
                    size: 18,
                    color: filled ? AppColors.orange : AppColors.border,
                  );
                }),
              ),
            ],
          ),
          if (review.comment.isNotEmpty) ...[
            const SizedBox(height: 10),
            Text(review.comment, style: const TextStyle(height: 1.4)),
          ],
        ],
      ),
    );
  }
}

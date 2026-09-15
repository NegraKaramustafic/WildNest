import 'package:ecommerce_mobile/models/activity.dart';
import 'package:ecommerce_mobile/models/activity_schedule.dart';
import 'package:ecommerce_mobile/providers/activity_provider.dart';
import 'package:ecommerce_mobile/providers/schedule_provider.dart';
import 'package:ecommerce_mobile/screens/activities/activity_detail_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/activity_card.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class ActivitiesListScreen extends StatefulWidget {
  const ActivitiesListScreen({super.key, this.showBack = true});

  final bool showBack;

  @override
  State<ActivitiesListScreen> createState() => _ActivitiesListScreenState();
}

class _ActivitiesListScreenState extends State<ActivitiesListScreen> {
  List<Activity> _activities = [];
  final Map<int, ActivitySchedule> _nextSlots = {};
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
      final activities = await activityProvider.getActive();
      List<ActivitySchedule> schedules = [];
      try {
        schedules = await scheduleProvider.getActiveUpcoming();
      } catch (_) {
        schedules = [];
      }
      if (!mounted) return;

      final next = <int, ActivitySchedule>{};
      for (final schedule in schedules) {
        if (!schedule.isBookable) continue;
        final current = next[schedule.activityId];
        if (current == null) {
          next[schedule.activityId] = schedule;
          continue;
        }
        if (schedule.isUpcoming && !current.isUpcoming) {
          next[schedule.activityId] = schedule;
          continue;
        }
        if (schedule.isUpcoming == current.isUpcoming &&
            schedule.startDateTime.isBefore(current.startDateTime)) {
          next[schedule.activityId] = schedule;
        }
      }

      setState(() {
        _activities = activities;
        _nextSlots
          ..clear()
          ..addAll(next);
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            WildNestHeader(
              showBack: widget.showBack,
              onBack: () => Navigator.pop(context),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
              child: Text(
                'Zoo Activities',
                style: GoogleFonts.playfairDisplay(
                  fontSize: 32,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            const Padding(
              padding: EdgeInsets.fromLTRB(20, 6, 20, 16),
              child: Text(
                'Book unforgettable wildlife experiences',
                style: TextStyle(color: AppColors.textMuted, fontSize: 15),
              ),
            ),
            Expanded(child: _body()),
          ],
        ),
      ),
    );
  }

  Widget _body() {
    if (_loading) {
      return const Center(
        child: CircularProgressIndicator(color: AppColors.sage),
      );
    }
    if (_error != null && _activities.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                _error!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textMuted),
              ),
              const SizedBox(height: 12),
              ElevatedButton(onPressed: _load, child: const Text('Try again')),
            ],
          ),
        ),
      );
    }
    if (_activities.isEmpty) {
      return const Center(
        child: Text(
          'No activities are available right now.',
          style: TextStyle(color: AppColors.textMuted),
        ),
      );
    }

    return RefreshIndicator(
      color: AppColors.sage,
      onRefresh: _load,
      child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 88),
        itemCount: _activities.length,
        separatorBuilder: (_, _) => const SizedBox(height: 12),
        itemBuilder: (context, index) {
          final activity = _activities[index];
          return ActivityCard(
            activity: activity,
            nextSchedule: _nextSlots[activity.id],
            onTap: () {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => ActivityDetailScreen(activityId: activity.id),
                ),
              );
            },
          );
        },
      ),
    );
  }
}

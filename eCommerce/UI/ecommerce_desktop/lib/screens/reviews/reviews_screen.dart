import 'dart:async';

import 'package:ecommerce_desktop/models/activity.dart';
import 'package:ecommerce_desktop/models/review.dart';
import 'package:ecommerce_desktop/providers/activity_provider.dart';
import 'package:ecommerce_desktop/providers/review_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/admin_list.dart';
import 'package:ecommerce_desktop/widgets/review_sentiment_section.dart';
import 'package:ecommerce_desktop/widgets/sentiment_badge.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ReviewsScreen extends StatefulWidget {
  const ReviewsScreen({super.key});

  @override
  State<ReviewsScreen> createState() => _ReviewsScreenState();
}

class _ReviewsScreenState extends State<ReviewsScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;
  int _sentimentReload = 0;
  bool _loading = true;
  bool _loadingActivities = true;
  String? _error;
  String? _sentiment;
  int? _activityId;
  int? _rating;
  DateTime? _dateFrom;
  DateTime? _dateTo;
  List<Review> _reviews = [];
  List<Activity> _activities = [];

  @override
  void initState() {
    super.initState();
    _loadActivities();
    _load();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadActivities() async {
    try {
      final result = await context.read<ActivityProvider>().get(
        filter: const {'Page': 1, 'PageSize': 100},
      );
      if (!mounted) return;
      final items = [...?result.items]..sort((a, b) => a.name.compareTo(b.name));
      setState(() {
        _activities = items;
        _loadingActivities = false;
      });
    } on SessionExpiredException {
      return;
    } catch (_) {
      if (!mounted) return;
      setState(() => _loadingActivities = false);
    }
  }

  Future<void> _load() async {
    if (_dateFrom != null &&
        _dateTo != null &&
        DateUtils.dateOnly(_dateFrom!).isAfter(DateUtils.dateOnly(_dateTo!))) {
      setState(() {
        _loading = false;
        _error = 'From date cannot be after to date.';
        _reviews = [];
        _total = 0;
      });
      return;
    }

    final requestId = ++_requestId;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await context.read<ReviewProvider>().get(
        filter: {
          'FTS': _searchController.text.trim(),
          'SentimentLabel': _sentiment,
          'ActivityId': _activityId,
          'Rating': _rating,
          'DateFrom': _dateFrom == null ? null : formatApiDate(_dateFrom!),
          'DateTo': _dateTo == null ? null : formatApiDate(_dateTo!),
          'Page': _page,
          'PageSize': _pageSize,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      final items = result.items ?? [];
      final total = result.totalCount ?? items.length;
      if (items.isEmpty && total > 0 && _page > 1) {
        setState(() => _page -= 1);
        await _load();
        return;
      }
      setState(() {
        _reviews = items;
        _total = total;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = friendlyError(error);
        _loading = false;
      });
    }
  }

  void _apply(VoidCallback change) {
    setState(() {
      change();
      _page = 1;
    });
    _load();
  }

  Future<void> _pickDate({required bool from}) async {
    final current = from ? _dateFrom : _dateTo;
    final today = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: current ?? DateTime(today.year, today.month, today.day),
      firstDate: DateTime(today.year - 5),
      lastDate: DateTime(today.year + 1, 12, 31),
    );
    if (picked == null || !mounted) return;
    final day = DateTime(picked.year, picked.month, picked.day);
    _apply(() {
      if (from) {
        _dateFrom = day;
      } else {
        _dateTo = day;
      }
    });
  }

  Future<void> _delete(Review review) async {
    final confirmed = await confirmAdminAction(
      context: context,
      title: 'Delete review',
      message: 'Delete this review? Sentiment statistics will be recalculated.',
      confirmLabel: 'Delete',
      destructive: true,
    );
    if (!confirmed || !mounted) return;
    try {
      await context.read<ReviewProvider>().remove(review.id);
      if (!mounted) return;
      _toast('Review deleted.');
      setState(() => _sentimentReload += 1);
      await _load();
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      _toast(friendlyError(error));
    }
  }

  void _toast(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(28, 28, 28, 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Reviews', style: TextStyle(fontSize: 26, fontWeight: FontWeight.w700)),
          const SizedBox(height: 4),
          const Text('Visitor comments and sentiment', style: TextStyle(color: AppColors.textMuted)),
          const SizedBox(height: 18),
          Expanded(
            child: ListView(
              children: [
                ReviewSentimentPanel(reloadToken: _sentimentReload),
                const SizedBox(height: 18),
                _filters(),
                const SizedBox(height: 16),
                SizedBox(height: 520, child: _table()),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _filters() {
    return Wrap(
      spacing: 12,
      runSpacing: 12,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        SizedBox(
          width: 280,
          child: TextField(
            controller: _searchController,
            onChanged: (_) {
              _debounce?.cancel();
              _debounce = Timer(const Duration(milliseconds: 300), () {
                if (!mounted) return;
                setState(() => _page = 1);
                _load();
              });
            },
            decoration: const InputDecoration(
              hintText: 'Search reviews...',
              prefixIcon: Icon(Icons.search),
              isDense: true,
            ),
          ),
        ),
        SizedBox(width: 220, child: _activityFilter()),
        SizedBox(width: 160, child: _ratingFilter()),
        SizedBox(width: 180, child: _sentimentFilter()),
        DateFilterField(
          label: 'From date',
          value: _dateFrom,
          onPick: () => _pickDate(from: true),
          onClear: _dateFrom == null ? null : () => _apply(() => _dateFrom = null),
        ),
        DateFilterField(
          label: 'To date',
          value: _dateTo,
          onPick: () => _pickDate(from: false),
          onClear: _dateTo == null ? null : () => _apply(() => _dateTo = null),
        ),
      ],
    );
  }

  Widget _activityFilter() {
    if (_loadingActivities) {
      return const InputDecorator(
        decoration: InputDecoration(labelText: 'Activity', isDense: true),
        child: Text('Loading...'),
      );
    }
    final selected = _activities.any((activity) => activity.id == _activityId) ? _activityId : null;
    return DropdownButtonFormField<int?>(
      key: ValueKey('review-activity-$selected-${_activities.length}'),
      initialValue: selected,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Activity', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('All activities', overflow: TextOverflow.ellipsis)),
        for (final activity in _activities)
          DropdownMenuItem(
            value: activity.id,
            child: Text(activity.name, overflow: TextOverflow.ellipsis),
          ),
      ],
      onChanged: (value) => _apply(() => _activityId = value),
    );
  }

  Widget _ratingFilter() {
    return DropdownButtonFormField<int?>(
      key: ValueKey('review-rating-$_rating'),
      initialValue: _rating,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Rating', isDense: true),
      items: const [
        DropdownMenuItem(value: null, child: Text('All ratings', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 5, child: Text('5', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 4, child: Text('4', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 3, child: Text('3', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 2, child: Text('2', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 1, child: Text('1', overflow: TextOverflow.ellipsis)),
      ],
      onChanged: (value) => _apply(() => _rating = value),
    );
  }

  Widget _sentimentFilter() {
    return DropdownButtonFormField<String?>(
      key: ValueKey('sentiment-$_sentiment'),
      initialValue: _sentiment,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Sentiment', isDense: true),
      items: const [
        DropdownMenuItem(value: null, child: Text('All sentiments', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'Positive', child: Text('Positive', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'Neutral', child: Text('Neutral', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'Negative', child: Text('Negative', overflow: TextOverflow.ellipsis)),
      ],
      onChanged: (value) => _apply(() => _sentiment = value),
    );
  }

  Widget _table() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) return ListError(message: _error!, onRetry: _load);
    const headerStyle = TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.textMuted);
    return AdminTableCard(
      minWidth: 1100,
      empty: 'No reviews to show.',
      pager: ListPager(
        page: _page,
        pageSize: _pageSize,
        total: _total,
        onPage: (page) {
          setState(() => _page = page);
          _load();
        },
      ),
      header: const Padding(
        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            Expanded(flex: 3, child: Text('Activity', style: headerStyle)),
            Expanded(flex: 2, child: Text('Author', style: headerStyle)),
            Expanded(flex: 1, child: Text('Rating', style: headerStyle)),
            Expanded(flex: 4, child: Text('Comment', style: headerStyle)),
            Expanded(flex: 2, child: Text('Sentiment', style: headerStyle)),
            Expanded(flex: 2, child: Text('Date', style: headerStyle)),
            SizedBox(width: 48, child: Text('Actions', style: headerStyle)),
          ],
        ),
      ),
      rows: [
        for (final review in _reviews)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                Expanded(flex: 3, child: EllipsisText(review.activityName.isEmpty ? '—' : review.activityName)),
                Expanded(flex: 2, child: EllipsisText(review.userFullName.isEmpty ? '—' : review.userFullName)),
                Expanded(flex: 1, child: Text('${review.rating}/5')),
                Expanded(flex: 4, child: EllipsisText(review.comment, maxLines: 2)),
                Expanded(
                  flex: 2,
                  child: Align(
                    alignment: Alignment.centerLeft,
                    child: SentimentBadge(label: review.sentimentLabel, score: review.sentimentScore),
                  ),
                ),
                Expanded(
                  flex: 2,
                  child: EllipsisText(review.createdAt == null ? '—' : formatDate(review.createdAt!.toLocal())),
                ),
                SizedBox(
                  width: 48,
                  child: IconActionButton(
                    icon: Icons.delete_outline,
                    color: AppColors.danger,
                    tooltip: 'Delete',
                    enabled: true,
                    onPressed: () => _delete(review),
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

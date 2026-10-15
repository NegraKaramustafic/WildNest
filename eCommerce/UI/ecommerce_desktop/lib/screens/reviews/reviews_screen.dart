import 'dart:async';

import 'package:ecommerce_desktop/models/review.dart';
import 'package:ecommerce_desktop/providers/review_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
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
  bool _loading = true;
  String? _error;
  String? _sentiment;
  List<Review> _reviews = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
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
          'Page': _page,
          'PageSize': _pageSize,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      final items = result.items ?? [];
      final total = result.totalCount ?? items.length;
      if (items.isEmpty && total > 0 && _page > 1) {
        setState(() => _page = _page - 1);
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
    } catch (e) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = e.toString().replaceFirst('Bad state: ', '');
        _loading = false;
      });
    }
  }

  void _onSearchChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), () {
      if (!mounted) return;
      setState(() => _page = 1);
      _load();
    });
  }

  void _changePage(int page) {
    setState(() => _page = page);
    _load();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(28, 28, 28, 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const _ReviewsHeader(),
          const SizedBox(height: 18),
          Expanded(
            child: ListView(
              children: [
                const ReviewSentimentPanel(),
                const SizedBox(height: 18),
                _filters(),
                const SizedBox(height: 16),
                SizedBox(height: 460, child: _table()),
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
          width: 320,
          child: TextField(
            controller: _searchController,
            onChanged: _onSearchChanged,
            decoration: const InputDecoration(
              hintText: 'Search reviews...',
              prefixIcon: Icon(Icons.search),
              isDense: true,
            ),
          ),
        ),
        SizedBox(
          width: 220,
          child: DropdownButtonFormField<String?>(
            key: ValueKey('sentiment-$_sentiment'),
            initialValue: _sentiment,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Sentiment', isDense: true),
            items: const [
              DropdownMenuItem(
                value: null,
                child: Text('All sentiments', overflow: TextOverflow.ellipsis),
              ),
              DropdownMenuItem(
                value: 'Positive',
                child: Text('Positive', overflow: TextOverflow.ellipsis),
              ),
              DropdownMenuItem(
                value: 'Neutral',
                child: Text('Neutral', overflow: TextOverflow.ellipsis),
              ),
              DropdownMenuItem(
                value: 'Negative',
                child: Text('Negative', overflow: TextOverflow.ellipsis),
              ),
            ],
            onChanged: (value) {
              setState(() {
                _sentiment = value;
                _page = 1;
              });
              _load();
            },
          ),
        ),
      ],
    );
  }

  Widget _table() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: _load, child: const Text('Try again')),
          ],
        ),
      );
    }

    final from = _total == 0 ? 0 : ((_page - 1) * _pageSize) + 1;
    final to = (_page * _pageSize) > _total ? _total : _page * _pageSize;

    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: 18, vertical: 14),
            child: Row(
              children: [
                Expanded(flex: 3, child: _HeaderLabel('Activity')),
                Expanded(flex: 2, child: _HeaderLabel('Author')),
                Expanded(flex: 1, child: _HeaderLabel('Rating')),
                Expanded(flex: 4, child: _HeaderLabel('Comment')),
                Expanded(flex: 2, child: _HeaderLabel('Sentiment')),
                Expanded(flex: 2, child: _HeaderLabel('Date')),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: _reviews.isEmpty
                ? const Center(
                    child: Text(
                      'No reviews to show.',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  )
                : ListView.separated(
                    itemCount: _reviews.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) => _ReviewRow(review: _reviews[index]),
                  ),
          ),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            child: Row(
              children: [
                Text(
                  _total == 0 ? '0 of 0' : '$from–$to of $_total',
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                ),
                const Spacer(),
                IconButton(
                  tooltip: 'Previous',
                  onPressed: _page > 1 ? () => _changePage(_page - 1) : null,
                  icon: const Icon(Icons.chevron_left),
                ),
                Text('$_page'),
                IconButton(
                  tooltip: 'Next',
                  onPressed: _page * _pageSize < _total ? () => _changePage(_page + 1) : null,
                  icon: const Icon(Icons.chevron_right),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ReviewsHeader extends StatelessWidget {
  const _ReviewsHeader();

  @override
  Widget build(BuildContext context) {
    return const Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Reviews',
          style: TextStyle(fontSize: 26, fontWeight: FontWeight.w700),
        ),
        SizedBox(height: 4),
        Text(
          'Visitor comments and sentiment',
          style: TextStyle(color: AppColors.textMuted),
        ),
      ],
    );
  }
}

class _HeaderLabel extends StatelessWidget {
  const _HeaderLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(
      text.toUpperCase(),
      maxLines: 1,
      overflow: TextOverflow.ellipsis,
      style: const TextStyle(
        fontSize: 11,
        letterSpacing: 0.6,
        fontWeight: FontWeight.w600,
        color: AppColors.textMuted,
      ),
    );
  }
}

class _ReviewRow extends StatelessWidget {
  const _ReviewRow({required this.review});

  final Review review;

  @override
  Widget build(BuildContext context) {
    final created = review.createdAt;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 10),
      child: Row(
        children: [
          Expanded(
            flex: 3,
            child: Text(
              review.activityName.isEmpty ? '—' : review.activityName,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
          ),
          Expanded(
            flex: 2,
            child: Text(
              review.userFullName.isEmpty ? '—' : review.userFullName,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
          ),
          Expanded(flex: 1, child: Text('${review.rating}/5')),
          Expanded(
            flex: 4,
            child: Text(
              review.comment,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
          ),
          Expanded(
            flex: 2,
            child: Align(
              alignment: Alignment.centerLeft,
              child: SentimentBadge(
                label: review.sentimentLabel,
                score: review.sentimentScore,
              ),
            ),
          ),
          Expanded(
            flex: 2,
            child: Text(
              created == null ? '—' : formatDate(created.toLocal()),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      ),
    );
  }
}

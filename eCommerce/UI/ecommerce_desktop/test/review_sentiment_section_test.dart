import 'package:ecommerce_desktop/models/review_sentiment_statistics.dart';
import 'package:ecommerce_desktop/theme/app_theme.dart';
import 'package:ecommerce_desktop/widgets/review_sentiment_section.dart';
import 'package:ecommerce_desktop/widgets/sentiment_badge.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  Widget host(Widget child) {
    return MaterialApp(
      theme: AppTheme.light,
      home: Scaffold(
        body: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: child,
        ),
      ),
    );
  }

  testWidgets('sentiment section shows loading, empty, and error states', (tester) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      host(const ReviewSentimentSection(loading: true)),
    );
    expect(find.text('Review sentiment'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    await tester.pumpWidget(
      host(
        ReviewSentimentSection(
          loading: false,
          statistics: const ReviewSentimentStatistics(),
        ),
      ),
    );
    expect(find.text('No reviews yet.'), findsOneWidget);

    await tester.pumpWidget(
      host(
        ReviewSentimentSection(
          loading: false,
          error: 'Could not load sentiment statistics.',
          onRetry: () {},
        ),
      ),
    );
    expect(find.text('Could not load sentiment statistics.'), findsOneWidget);
    expect(find.text('Try again'), findsOneWidget);
  });

  testWidgets('sentiment section shows labels and the badge text', (tester) async {
    tester.view.physicalSize = const Size(1200, 1400);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    const statistics = ReviewSentimentStatistics(
      totalReviews: 18,
      labeledReviews: 18,
      overallAverageRating: 3.8,
      labels: [
        SentimentLabelStatistic(label: 'Positive', count: 10, percentage: 55.6, averageRating: 4.8),
        SentimentLabelStatistic(label: 'Neutral', count: 4, percentage: 22.2, averageRating: 3),
        SentimentLabelStatistic(label: 'Negative', count: 4, percentage: 22.2, averageRating: 1.5),
      ],
      monthlyTrend: [
        SentimentTrendPoint(year: 2026, month: 4, positiveCount: 2, neutralCount: 1, negativeCount: 1),
      ],
      activities: [
        SentimentActivityBreakdown(
          activityId: 1,
          activityName: 'Elephant Feeding Experience',
          positiveCount: 3,
          neutralCount: 0,
          negativeCount: 0,
          averageRating: 4.7,
        ),
      ],
    );

    await tester.pumpWidget(
      host(
        const Column(
          children: [
            ReviewSentimentSection(loading: false, statistics: statistics),
            SentimentBadge(label: 'Positive', score: 0.92),
            SentimentBadge(label: null),
          ],
        ),
      ),
    );
    await tester.pump();

    expect(find.text('Positive'), findsWidgets);
    expect(find.text('Neutral'), findsWidgets);
    expect(find.text('Negative'), findsWidgets);
    expect(find.text('Distribution'), findsOneWidget);
    expect(find.text('Monthly trend'), findsOneWidget);
    expect(find.text('Elephant Feeding Experience'), findsOneWidget);
    expect(find.text('Unlabeled'), findsOneWidget);
  });
}

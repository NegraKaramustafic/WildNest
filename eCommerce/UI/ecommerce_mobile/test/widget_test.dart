import 'package:ecommerce_mobile/main.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('WildNest app builds', (WidgetTester tester) async {
    await tester.pumpWidget(const WildNestApp());
    await tester.pump();
    expect(find.text('WildNest'), findsWidgets);
  });
}

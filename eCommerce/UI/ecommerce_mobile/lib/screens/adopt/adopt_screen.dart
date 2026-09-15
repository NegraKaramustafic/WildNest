import 'package:ecommerce_mobile/widgets/coming_soon.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';

class AdoptScreen extends StatelessWidget {
  const AdoptScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const SafeArea(
      child: Column(
        children: [
          WildNestHeader(),
          Expanded(
            child: ComingSoonView(
              title: 'Adopt virtually',
              message:
                  'Virtual adoptions will be available here in a later session.',
              emoji: '💚',
            ),
          ),
        ],
      ),
    );
  }
}

import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/screens/news/news_screen.dart';
import 'package:ecommerce_desktop/screens/activities/activities_screen.dart';
import 'package:ecommerce_desktop/screens/dashboard/dashboard_screen.dart';
import 'package:ecommerce_desktop/screens/reports/reports_screen.dart';
import 'package:ecommerce_desktop/screens/activities/activity_types_screen.dart';
import 'package:ecommerce_desktop/screens/adoptions/adoptions_screen.dart';
import 'package:ecommerce_desktop/screens/animals/animals_screen.dart';
import 'package:ecommerce_desktop/screens/badges/badges_screen.dart';
import 'package:ecommerce_desktop/screens/donations/donations_screen.dart';
import 'package:ecommerce_desktop/screens/partners/zoo_partners_screen.dart';
import 'package:ecommerce_desktop/screens/reservations/reservations_screen.dart';
import 'package:ecommerce_desktop/screens/reviews/reviews_screen.dart';
import 'package:ecommerce_desktop/screens/schedules/schedules_screen.dart';
import 'package:ecommerce_desktop/screens/tickets/ticket_verification_screen.dart';
import 'package:ecommerce_desktop/screens/users/users_screen.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

enum AdminSection {
  dashboard('Dashboard', Icons.dashboard_outlined),
  reports('Reports', Icons.picture_as_pdf_outlined),
  animals('Animals', Icons.pets_outlined),
  activities('Activities', Icons.local_activity_outlined),
  activityTypes('Activity types', Icons.label_outline),
  schedules('Schedules', Icons.calendar_month_outlined),
  reservations('Reservations', Icons.event_available_outlined),
  donations('Donations', Icons.favorite_border),
  adoptions('Adoptions', Icons.volunteer_activism_outlined),
  users('Users', Icons.people_outline),
  reviews('Reviews', Icons.chat_bubble_outline),
  badges('Badges', Icons.emoji_events_outlined),
  zooPartners('Zoo partners', Icons.park_outlined),
  news('News', Icons.article_outlined),
  tickets('Tickets', Icons.confirmation_number_outlined);

  const AdminSection(this.label, this.icon);

  final String label;
  final IconData icon;
}

class AdminShell extends StatefulWidget {
  const AdminShell({super.key});

  @override
  State<AdminShell> createState() => _AdminShellState();
}

class _AdminShellState extends State<AdminShell> {
  AdminSection _section = AdminSection.dashboard;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: Row(
        children: [
          _Sidebar(
            selected: _section,
            onSelected: (section) => setState(() => _section = section),
          ),
          Expanded(
            child: Column(
              children: [
                const _TopBar(),
                Expanded(child: _content()),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _content() {
    switch (_section) {
      case AdminSection.dashboard:
        return const DashboardScreen();
      case AdminSection.reports:
        return const ReportsScreen();
      case AdminSection.animals:
        return const AnimalsScreen();
      case AdminSection.activities:
        return const ActivitiesScreen();
      case AdminSection.activityTypes:
        return const ActivityTypesScreen();
      case AdminSection.schedules:
        return const SchedulesScreen();
      case AdminSection.reservations:
        return const ReservationsScreen();
      case AdminSection.donations:
        return const DonationsScreen();
      case AdminSection.adoptions:
        return const AdoptionsScreen();
      case AdminSection.tickets:
        return const TicketVerificationScreen();
      case AdminSection.reviews:
        return const ReviewsScreen();
      case AdminSection.users:
        return const UsersScreen();
      case AdminSection.badges:
        return const BadgesScreen();
      case AdminSection.zooPartners:
        return const ZooPartnersScreen();
      case AdminSection.news:
        return const NewsScreen();
    }
  }
}

class _Sidebar extends StatelessWidget {
  const _Sidebar({required this.selected, required this.onSelected});

  final AdminSection selected;
  final ValueChanged<AdminSection> onSelected;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 248,
      color: AppColors.sidebar,
      child: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Padding(
              padding: EdgeInsets.fromLTRB(18, 18, 18, 16),
              child: _Brand(),
            ),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                children: [
                  for (final section in AdminSection.values)
                    _NavItem(
                      section: section,
                      selected: section == selected,
                      onTap: () => onSelected(section),
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

class _Brand extends StatelessWidget {
  const _Brand();

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 32,
          height: 32,
          decoration: BoxDecoration(
            color: AppColors.green,
            borderRadius: BorderRadius.circular(8),
          ),
          child: const Icon(Icons.eco, color: Colors.white, size: 18),
        ),
        const SizedBox(width: 10),
        const Flexible(
          child: Text(
            'WildNest',
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              color: Colors.white,
              fontSize: 16,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
        const SizedBox(width: 8),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
          decoration: BoxDecoration(
            color: const Color(0xFF2E6848),
            borderRadius: BorderRadius.circular(20),
          ),
          child: const Text(
            'Admin',
            style: TextStyle(
              color: Color(0xFFD7F5E2),
              fontSize: 11,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ],
    );
  }
}

class _NavItem extends StatelessWidget {
  const _NavItem({
    required this.section,
    required this.selected,
    required this.onTap,
  });

  final AdminSection section;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 2),
      child: Material(
        color: selected ? AppColors.sidebarSelected : Colors.transparent,
        borderRadius: BorderRadius.circular(8),
        child: InkWell(
          borderRadius: BorderRadius.circular(8),
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 10),
            child: Row(
              children: [
                Icon(section.icon, size: 18, color: AppColors.sidebarText),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    section.label,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: AppColors.sidebarText,
                      fontSize: 13.5,
                      fontWeight: selected ? FontWeight.w600 : FontWeight.w500,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _TopBar extends StatelessWidget {
  const _TopBar();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 56,
      padding: const EdgeInsets.symmetric(horizontal: 28),
      decoration: const BoxDecoration(
        color: AppColors.surface,
        border: Border(bottom: BorderSide(color: AppColors.border)),
      ),
      child: Row(
        children: [
          const Text(
            'Admin panel',
            style: TextStyle(
              color: AppColors.textMuted,
              fontSize: 14,
              fontWeight: FontWeight.w500,
            ),
          ),
          const Spacer(),
          TextButton.icon(
            onPressed: () => _confirmLogout(context),
            icon: const Icon(Icons.logout, size: 18),
            label: const Text('Sign out'),
          ),
        ],
      ),
    );
  }

  Future<void> _confirmLogout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Sign out'),
        content: const Text('Do you want to sign out?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Sign out'),
          ),
        ],
      ),
    );
    if (confirmed == true && context.mounted) {
      await context.read<AuthProvider>().logout();
    }
  }
}

import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/screens/activities/activities_screen.dart';
import 'package:ecommerce_desktop/screens/activities/activity_types_screen.dart';
import 'package:ecommerce_desktop/screens/animals/animals_screen.dart';
import 'package:ecommerce_desktop/screens/placeholder_page.dart';
import 'package:ecommerce_desktop/screens/schedules/schedules_screen.dart';
import 'package:ecommerce_desktop/screens/tickets/ticket_verification_screen.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

enum AdminSection {
  dashboard('Kontrolna ploča', Icons.dashboard_outlined),
  animals('Životinje', Icons.pets_outlined),
  activities('Aktivnosti', Icons.local_activity_outlined),
  activityTypes('Tipovi aktivnosti', Icons.label_outline),
  schedules('Termini', Icons.calendar_month_outlined),
  reservations('Rezervacije', Icons.event_available_outlined),
  donations('Donacije', Icons.favorite_border),
  adoptions('Usvajanja', Icons.volunteer_activism_outlined),
  users('Korisnici', Icons.people_outline),
  reviews('Recenzije', Icons.chat_bubble_outline),
  badges('Značke', Icons.emoji_events_outlined),
  reports('Izvještaji', Icons.description_outlined),
  tickets('Verifikacija ulaznica', Icons.confirmation_number_outlined),
  settings('Postavke', Icons.settings_outlined);

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
      case AdminSection.animals:
        return const AnimalsScreen();
      case AdminSection.activities:
        return const ActivitiesScreen();
      case AdminSection.activityTypes:
        return const ActivityTypesScreen();
      case AdminSection.schedules:
        return const SchedulesScreen();
      case AdminSection.tickets:
        return const TicketVerificationScreen();
      default:
        return PlaceholderPage(title: _section.label);
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
            label: const Text('Odjava'),
          ),
        ],
      ),
    );
  }

  Future<void> _confirmLogout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Odjava'),
        content: const Text('Želite li se odjaviti?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Odustani'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Odjavi se'),
          ),
        ],
      ),
    );
    if (confirmed == true && context.mounted) {
      await context.read<AuthProvider>().logout();
    }
  }
}

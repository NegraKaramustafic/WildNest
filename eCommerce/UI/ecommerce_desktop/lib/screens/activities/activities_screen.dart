import 'dart:async';

import 'package:ecommerce_desktop/models/activity.dart';
import 'package:ecommerce_desktop/providers/activity_provider.dart';
import 'package:ecommerce_desktop/screens/activities/activity_form_dialog.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/status_chip.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ActivitiesScreen extends StatefulWidget {
  const ActivitiesScreen({super.key});

  @override
  State<ActivitiesScreen> createState() => _ActivitiesScreenState();
}

class _ActivitiesScreenState extends State<ActivitiesScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;

  ActivityType? _type;
  ActivityStatus? _status;

  bool _loading = true;
  String? _error;
  List<Activity> _activities = [];

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
      final provider = context.read<ActivityProvider>();
      final result = await provider.get(
        filter: {
          'Name': _searchController.text.trim(),
          'Type': _type == null ? null : activityTypeApiValue(_type!),
          'Status': _status == null ? null : activityStatusApiValue(_status!),
          'Page': _page,
          'PageSize': _pageSize,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _activities = result.items ?? [];
        _total = result.totalCount ?? _activities.length;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = _message(e);
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

  Future<void> _add() async {
    final result = await showActivityFormDialog(context);
    if (result == null || !mounted) return;
    if (result.created != null) {
      await _offerActivate(result.created!);
    }
    if (mounted) await _load();
  }

  Future<void> _offerActivate(Activity activity) async {
    final activate = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Aktivirati sada?'),
        content: Text(
          'Aktivnost „${activity.name}” je sačuvana kao nacrt. Želite li je aktivirati sada?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Ne sada'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Aktiviraj sada'),
          ),
        ],
      ),
    );
    if (activate != true || !mounted) return;

    try {
      final provider = context.read<ActivityProvider>();
      await provider.activate(activity.id);
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  Future<void> _edit(Activity activity) async {
    final result = await showActivityFormDialog(context, activity: activity);
    if (result != null && mounted) await _load();
  }

  Future<void> _activate(Activity activity) async {
    try {
      final provider = context.read<ActivityProvider>();
      await provider.activate(activity.id);
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  Future<void> _deactivate(Activity activity) async {
    try {
      final provider = context.read<ActivityProvider>();
      await provider.deactivate(activity.id);
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  Future<void> _delete(Activity activity) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Brisanje aktivnosti'),
        content: Text('Obrisati aktivnost ${activity.name}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Odustani'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Obriši'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      final provider = context.read<ActivityProvider>();
      await provider.remove(activity.id);
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  String _message(Object error) {
    final text = error.toString();
    return text.replaceFirst('Bad state: ', '');
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Upravljanje aktivnostima',
                      style: TextStyle(
                        fontSize: 26,
                        fontWeight: FontWeight.w700,
                        color: AppColors.text,
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'Pregled i uređivanje aktivnosti',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  ],
                ),
              ),
              FilledButton.icon(
                onPressed: _add,
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Dodaj aktivnost'),
              ),
            ],
          ),
          const SizedBox(height: 18),
          Wrap(
            spacing: 12,
            runSpacing: 12,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              SizedBox(
                width: 280,
                child: TextField(
                  controller: _searchController,
                  onChanged: _onSearchChanged,
                  decoration: const InputDecoration(
                    hintText: 'Pretraži po nazivu...',
                    prefixIcon: Icon(Icons.search),
                    isDense: true,
                  ),
                ),
              ),
              SizedBox(width: 200, child: _typeFilter()),
              SizedBox(width: 200, child: _statusFilter()),
            ],
          ),
          const SizedBox(height: 16),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _typeFilter() {
    return DropdownButtonFormField<ActivityType?>(
      key: ValueKey('type-$_type'),
      initialValue: _type,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Tip', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('Svi tipovi')),
        for (final type in ActivityType.values)
          DropdownMenuItem(value: type, child: Text(activityTypeLabel(type))),
      ],
      onChanged: (value) {
        setState(() {
          _type = value;
          _page = 1;
        });
        _load();
      },
    );
  }

  Widget _statusFilter() {
    return DropdownButtonFormField<ActivityStatus?>(
      key: ValueKey('status-$_status'),
      initialValue: _status,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Status', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('Svi statusi')),
        for (final status in ActivityStatus.values)
          DropdownMenuItem(value: status, child: Text(activityStatusLabel(status))),
      ],
      onChanged: (value) {
        setState(() {
          _status = value;
          _page = 1;
        });
        _load();
      },
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return _ErrorState(message: _error!, onRetry: _load);
    }
    return _ActivitiesTable(
      activities: _activities,
      page: _page,
      pageSize: _pageSize,
      total: _total,
      onPage: _changePage,
      onEdit: _edit,
      onActivate: _activate,
      onDeactivate: _deactivate,
      onDelete: _delete,
    );
  }
}

class _ActivitiesTable extends StatelessWidget {
  const _ActivitiesTable({
    required this.activities,
    required this.page,
    required this.pageSize,
    required this.total,
    required this.onPage,
    required this.onEdit,
    required this.onActivate,
    required this.onDeactivate,
    required this.onDelete,
  });

  final List<Activity> activities;
  final int page;
  final int pageSize;
  final int total;
  final ValueChanged<int> onPage;
  final ValueChanged<Activity> onEdit;
  final ValueChanged<Activity> onActivate;
  final ValueChanged<Activity> onDeactivate;
  final ValueChanged<Activity> onDelete;

  @override
  Widget build(BuildContext context) {
    final from = total == 0 ? 0 : ((page - 1) * pageSize) + 1;
    final to = (page * pageSize) > total ? total : page * pageSize;
    final hasPrev = page > 1;
    final hasNext = page * pageSize < total;

    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          const _HeaderRow(),
          const Divider(height: 1),
          Expanded(
            child: activities.isEmpty
                ? const Center(
                    child: Text(
                      'Nema aktivnosti za prikaz.',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  )
                : ListView.separated(
                    itemCount: activities.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final activity = activities[index];
                      return _ActivityRow(
                        activity: activity,
                        onEdit: () => onEdit(activity),
                        onActivate: () => onActivate(activity),
                        onDeactivate: () => onDeactivate(activity),
                        onDelete: () => onDelete(activity),
                      );
                    },
                  ),
          ),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            child: Row(
              children: [
                Text(
                  total == 0 ? '0 od 0' : '$from–$to od $total',
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                ),
                const Spacer(),
                IconButton(
                  tooltip: 'Prethodna',
                  onPressed: hasPrev ? () => onPage(page - 1) : null,
                  icon: const Icon(Icons.chevron_left),
                ),
                Text('$page'),
                IconButton(
                  tooltip: 'Sljedeća',
                  onPressed: hasNext ? () => onPage(page + 1) : null,
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

class _HeaderRow extends StatelessWidget {
  const _HeaderRow();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.symmetric(horizontal: 18, vertical: 14),
      child: Row(
        children: [
          Expanded(flex: 4, child: _HeaderLabel('Aktivnost')),
          Expanded(flex: 2, child: _HeaderLabel('Tip')),
          Expanded(flex: 2, child: _HeaderLabel('Životinja')),
          Expanded(flex: 2, child: _HeaderLabel('Trajanje')),
          Expanded(flex: 2, child: _HeaderLabel('Cijena')),
          Expanded(flex: 2, child: _HeaderLabel('Kapacitet')),
          Expanded(flex: 2, child: _HeaderLabel('Status')),
          SizedBox(width: 156, child: _HeaderLabel('Akcije')),
        ],
      ),
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
      style: const TextStyle(
        fontSize: 11,
        letterSpacing: 0.6,
        fontWeight: FontWeight.w600,
        color: AppColors.textMuted,
      ),
    );
  }
}

class _ActivityRow extends StatelessWidget {
  const _ActivityRow({
    required this.activity,
    required this.onEdit,
    required this.onActivate,
    required this.onDeactivate,
    required this.onDelete,
  });

  final Activity activity;
  final VoidCallback onEdit;
  final VoidCallback onActivate;
  final VoidCallback onDeactivate;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    final canEdit = activity.allows(ActivityActions.update);
    final canActivate = activity.allows(ActivityActions.activate);
    final canDeactivate = activity.allows(ActivityActions.deactivate);
    final canDelete = activity.allows(ActivityActions.delete);

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
      child: Row(
        children: [
          Expanded(
            flex: 4,
            child: Row(
              children: [
                _Thumbnail(url: activity.imageUrl),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    activity.name,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                ),
              ],
            ),
          ),
          Expanded(flex: 2, child: Text(activityTypeLabel(activity.type))),
          Expanded(
            flex: 2,
            child: Text(
              activity.animalName?.trim().isNotEmpty == true ? activity.animalName! : '—',
              overflow: TextOverflow.ellipsis,
            ),
          ),
          Expanded(flex: 2, child: Text(formatDurationLabel(activity.duration))),
          Expanded(flex: 2, child: Text(formatKm(activity.price))),
          Expanded(flex: 2, child: Text('${activity.capacity}')),
          Expanded(
            flex: 2,
            child: Align(
              alignment: Alignment.centerLeft,
              child: _statusChip(activity.status),
            ),
          ),
          SizedBox(
            width: 156,
            child: Row(
              children: [
                _IconAction(
                  icon: Icons.edit_outlined,
                  color: AppColors.textMuted,
                  tooltip: canEdit ? 'Uredi' : _disabledTooltip(ActivityActions.update),
                  enabled: canEdit,
                  onPressed: onEdit,
                ),
                const SizedBox(width: 4),
                _IconAction(
                  icon: Icons.play_arrow_outlined,
                  color: AppColors.green,
                  tooltip: canActivate ? 'Aktiviraj' : _disabledTooltip(ActivityActions.activate),
                  enabled: canActivate,
                  onPressed: onActivate,
                ),
                const SizedBox(width: 4),
                _IconAction(
                  icon: Icons.pause_outlined,
                  color: AppColors.warningText,
                  tooltip: canDeactivate
                      ? 'Deaktiviraj'
                      : _disabledTooltip(ActivityActions.deactivate),
                  enabled: canDeactivate,
                  onPressed: onDeactivate,
                ),
                const SizedBox(width: 4),
                _IconAction(
                  icon: Icons.delete_outline,
                  color: AppColors.danger,
                  tooltip: canDelete ? 'Obriši' : _disabledTooltip(ActivityActions.delete),
                  enabled: canDelete,
                  onPressed: onDelete,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _disabledTooltip(String action) {
    switch (action) {
      case ActivityActions.update:
        if (activity.status == ActivityStatus.active) {
          return 'Aktivnu aktivnost prvo deaktivirajte prije uređivanja.';
        }
        return 'Samo nacrt se može uređivati.';
      case ActivityActions.activate:
        return 'Aktivnost je već aktivna.';
      case ActivityActions.deactivate:
        if (activity.status == ActivityStatus.draft) {
          return 'Nacrt se aktivira, a ne deaktivira.';
        }
        return 'Aktivnost je već neaktivna.';
      case ActivityActions.delete:
        if (activity.status == ActivityStatus.active) {
          return 'Aktivnu aktivnost nije moguće obrisati. Prvo je deaktivirajte.';
        }
        return 'Samo nacrt se može obrisati.';
      default:
        return 'Akcija nije dozvoljena.';
    }
  }
}

Widget _statusChip(ActivityStatus status) {
  switch (status) {
    case ActivityStatus.active:
      return const StatusChip(active: true, label: 'Aktivna');
    case ActivityStatus.inactive:
      return const StatusChip(active: false, label: 'Neaktivna');
    case ActivityStatus.draft:
      return const StatusChip(
        active: false,
        label: 'Nacrt',
        background: Color(0xFFE8E6E1),
        foreground: AppColors.textMuted,
      );
  }
}

class _Thumbnail extends StatelessWidget {
  const _Thumbnail({required this.url});

  final String? url;

  @override
  Widget build(BuildContext context) {
    final image = url?.trim();
    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: SizedBox(
        width: 42,
        height: 42,
        child: image == null || image.isEmpty
            ? const ColoredBox(
                color: AppColors.border,
                child: Icon(Icons.image_outlined, size: 18, color: AppColors.textMuted),
              )
            : Image.network(
                image,
                fit: BoxFit.cover,
                errorBuilder: (_, _, _) => const ColoredBox(
                  color: AppColors.border,
                  child: Icon(Icons.broken_image_outlined, size: 18, color: AppColors.textMuted),
                ),
              ),
      ),
    );
  }
}

class _IconAction extends StatelessWidget {
  const _IconAction({
    required this.icon,
    required this.color,
    required this.tooltip,
    required this.enabled,
    required this.onPressed,
  });

  final IconData icon;
  final Color color;
  final String tooltip;
  final bool enabled;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: InkWell(
        onTap: enabled ? onPressed : () {},
        borderRadius: BorderRadius.circular(8),
        child: Container(
          width: 34,
          height: 34,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: AppColors.border),
          ),
          child: Icon(
            icon,
            size: 18,
            color: enabled ? color : color.withValues(alpha: 0.35),
          ),
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton(onPressed: onRetry, child: const Text('Pokušaj ponovo')),
        ],
      ),
    );
  }
}

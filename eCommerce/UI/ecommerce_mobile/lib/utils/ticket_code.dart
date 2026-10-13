final RegExp wildNestTicketCode = RegExp(r'^WN-[A-Z0-9]{6}$');

String normalizeTicketCode(String raw) => raw.trim().toUpperCase();

bool isWildNestTicketCode(String raw) {
  return wildNestTicketCode.hasMatch(normalizeTicketCode(raw));
}

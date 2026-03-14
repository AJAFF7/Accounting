# Changelog Generation Template

## Format Preferences

### Structure
- Generate HTML content without `<html>` or `<body>` tags
- Use MudBlazor CSS utilities and typography classes
- Root container: `<div class="d-flex flex-column">`
- Use flexbox for layout with minimal gaps (`gap-1`)

### Sections to Include
1. **New Features** - New functionality added
2. **Improvements** - Enhancements, bug fixes, and optimizations
3. **Notes** - Important information for end users (requirements, compatibility)

### Sections to EXCLUDE
- Version number (already in dialog title)
- Release date
- Modified Files section
- Technical/sensitive implementation details
- Database migration details (unless critical)

### Styling Guidelines
- Use `<p class="mud-typography mud-typography-body1 mb-1">` for section headers
- Use `<div class="d-flex flex-column gap-1 mb-3 pl-4">` for lists
- Use simple `<div>` elements with bullet points (•) instead of `<ul>/<li>`
- Keep typography classes simple: `mud-typography-body1` for headers, `mud-typography-body2` for notes
- Minimal spacing between items

### Content Guidelines
- Write for end users, not developers
- Avoid technical jargon
- Use clear, concise language
- Focus on user-facing changes
- No sensitive file paths or implementation details

## Example Output

```html
<div class="d-flex flex-column">
    <p class="mud-typography mud-typography-body1 mb-1"><strong>New Features:</strong></p>
    <div class="d-flex flex-column gap-1 mb-3 pl-4">
        <div>• Added role-based access control with Company role</div>
        <div>• Organization validation for company registration</div>
        <div>• Auto-assign organization ID on company creation</div>
    </div>

    <p class="mud-typography mud-typography-body1 mb-1"><strong>Improvements:</strong></p>
    <div class="d-flex flex-column gap-1 mb-3 pl-4">
        <div>• Enhanced security with user-organization validation</div>
        <div>• Removed unused imports and cleaned up code</div>
        <div>• Updated error messaging</div>
    </div>

    <p class="mud-typography mud-typography-body2"><strong>Notes:</strong> Users must have Company role and be in exactly one organization. No migration required.</p>
</div>
```

## Usage

When requesting changelog generation, provide this template file to maintain consistency across all changelog entries.

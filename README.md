# Invoicing-CRUD

An invoice manager built with Blazor Server (.NET 10), Radzen components, EF Core with SQLite, and ASP.NET Core Identity. You can create invoices with line items, calculate discounts and tax, move invoices through a status workflow, and track them on a dashboard.

## Architecture decision

I could have used a layered design where each part is its own .NET project (for example `InvoiceApp.Domain`, `InvoiceApp.Application`, `InvoiceApp.Infrastructure` and `InvoiceApp.Web`), each with its own `.csproj` and project references. For an app this size, that would add build setup and project references without much benefit, so I kept the same separation as **folders inside one project** instead:

| Folder | Layer | Contents |
|---|---|---|
| `Domain/` | Domain | Entities, status rules, calculations. No dependencies on EF Core or UI. |
| `Features/` | Application | Services, DTOs, validators and interfaces, grouped by feature (Invoices, Account). |
| `Data/` | Infrastructure | EF Core `DbContext`, entity configurations, repositories, migrations, seeding. |
| `Components/` | Presentation | Blazor pages and shared components. |

The layers and the dependency direction are the same as in the multi-project version (Components → Features → Domain, with Data implementing the Features interfaces). Only the compiler doesn't enforce the boundaries. If the app grows, each folder can be moved into its own project without redesigning it.

## State management and data binding

The app runs as Blazor Server with interactive server rendering, so every page keeps its state on the server for the user's connection (the circuit). I didn't add a global state store: each page owns its own state and reloads from the database when it opens. For an app with a few independent pages, a shared store would add complexity without solving a real problem.

**Form models are separate from DTOs.** Each form binds to its own UI model (for example `InvoiceFormModel` and `LineItemFormModel`), which uses data annotations for quick checks in the browser. When you save, the model is converted into a DTO (`ToDto()`) and sent to a service. The service runs the FluentValidation rules, which are the real source of truth. The domain entities are never bound to the UI directly, so a half-edited form can't change an entity.

**Binding inside the invoice form.** The invoice page is split into child components, and the parent holds the state:

- `InvoiceHeaderForm` binds the customer, dates and tax rate with two-way `@bind-Value`. The date pickers work with `DateTime?`, so the component converts to and from the model's `DateOnly`. When the issue date moves past the due date, it moves the due date to issue date + 30 days.
- `LineItemsGrid` edits the parent's list of line items in place using Radzen's inline row editing. Before a row is edited it keeps a copy, so Cancel can restore the old values.
- `InvoiceTotals` gets the line items and tax rate as parameters and calculates Subtotal, Tax and Total on every render with the same `InvoiceCalculations` code the server uses, so the totals update while you type.
- Each child reports changes to the parent through an `EventCallback OnChange`. The parent uses it to set a `_hasUnsavedChanges` flag. A `NavigationLock` reads that flag: it asks before you leave the page inside the app, and the browser asks before you close or refresh the tab.

**Loading and reloading data.**

- Pages load their data in lifecycle methods. The invoice form uses `OnParametersSetAsync` and remembers which id it loaded, so going from `/invoices/new` to `/invoices/{id}` after a save loads the new invoice instead of reusing the old state.
- The invoice and customer lists use server-side paging and sorting through Radzen's `LoadData` event, so only one page of rows is loaded at a time. The search box waits 300 ms after you stop typing before it reloads.
- The customer dropdown reloads its list every time it opens, so a customer added in another tab shows up.
- After a save, delete or status change, the page reloads the affected data from the server instead of patching its local copy, so what you see is always what's stored.

**Database access.** Repositories get a new, short-lived `DbContext` from `IDbContextFactory` for each operation instead of sharing one. A Blazor Server circuit can stay open for hours, and a single long-lived `DbContext` would build up tracked entities and isn't safe to use from overlapping async calls. Read queries use `AsNoTracking()` and select straight into DTOs.

**Errors.** Services return a `Result` (success, or a message) instead of throwing for business rule failures. Pages show the message in a toast or an inline alert, so the user always sees why something didn't work.

**Settings.** Paging defaults (page size and the page size options) come from `appsettings.json` through the options pattern (`PaginationSettings`) instead of being hard-coded in each page.

## UX choices

- **One page to create, edit and view an invoice.** `/invoices/new` and `/invoices/{id}` use the same form. When an invoice is no longer a Draft, the same page opens read-only, with the fields disabled and only a Back to List button. The user always sees an invoice laid out the same way, and the "only drafts can be edited" rule is visible instead of being an error after the fact.
- **Line items are edited inside the grid.** You add, edit and remove rows directly in the table instead of opening a separate dialog for each item, which is faster when an invoice has many lines. Only one row can be open at a time, so it's always clear which row you're changing.
- **Totals update as you type**, so you can check the amounts before saving.
- **Two save buttons.** "Save & Continue" keeps you on the invoice so you can keep working. "Save Invoice" / "Save Changes" returns you to the list.
- **The row menu shows only what you can do.** Each row's ⋮ menu is built from the invoice's status: Edit and Delete appear only for drafts, and Change Status offers only the next allowed statuses. The user can't pick an action that would fail.
- **Confirmation only for actions you can't undo.** Deleting an invoice or customer, and marking an invoice Paid or Cancelled, ask for confirmation. Everyday actions like saving don't, so the dialogs keep their meaning.
- **Protection against losing work.** Leaving an invoice or closing a customer dialog with unsaved changes asks first.
- **Customers are edited in a dialog.** A customer has only a few fields, so a dialog over the list is quicker than a separate page, and you stay where you were in the list.
- **Clear feedback.** Every save, delete and status change shows a toast with the result. Validation errors from the server appear as a toast on the invoice form and as an inline alert in the customer and profile forms. Buttons show a busy state while saving, so a double-click doesn't submit twice.
- **Status is visible at a glance.** Statuses are colored badges in the list, overdue due dates are shown in red, and the dashboard cards count invoices by status.
- **Helpful empty states.** An empty list explains what to do next: "Create your first invoice to get started" when there's no data, or "Try adjusting your search or filter criteria" when a search finds nothing.
- **Search that fits how people look for invoices.** One box searches by customer name, company name or invoice number, and the number works as `INV-00012`, `inv-12` or `12`.

## Getting started

Requirements: .NET 10 SDK.

```bash
dotnet tool restore
dotnet run --project src/InvoiceApp
```

Open http://localhost:5206 (or https://localhost:7259) and log in with the demo account:

- Email: `admin@test.com`
- Password: `12345678`

In Development the SQLite database (`src/InvoiceApp/invoices.db`) is **deleted and re-seeded on every start**, with 10 customers and sample invoices in every status. Anything you create is gone after a restart.

Run the automated tests:

```bash
dotnet test
```

## Business rules

- A new invoice is always a **Draft**.
- Status flow: Draft → Sent or Cancelled; Sent → Paid or Cancelled. **Paid** and **Cancelled** are final.
- Only Draft invoices can be edited or deleted.
- An invoice needs at least one line item before it can be sent.
- Discounts are **per line item** (0–100%). There is no invoice-level discount.
- Line total = quantity × unit price − discount, each rounded to 2 decimals (away from zero). Subtotal = sum of line totals. Tax = subtotal × tax rate, rounded to 2 decimals. Total = subtotal + tax.
- A Sent invoice whose due date has passed is shown as **overdue** (due date in red).
- Invoice numbers are `INV-` + the id padded to 5 digits (e.g. `INV-00012`).

## Tested scenarios

The app was tested manually against the 94 scenarios below, grouped by feature. Within each feature they go from the main flow to validation, edge cases, and complex cases (multiple tabs, failures).

Test setup: logged in with the demo account on the seeded data, which has customers with and without a company name and invoices in every status, including an overdue Sent invoice. Multi-tab scenarios used two browser tabs (A and B) on the same invoice.

### Create invoice

Invoices → New Invoice (/invoices/new). A new invoice is always saved as Draft.

#### Happy path

- **CRT-01 · Create a basic invoice**
  - Steps: Click New Invoice. Pick a customer, keep the dates, set tax 11. Add one line item and click ✓ on the row. Click Save Invoice.
  - Expected: Toast "Invoice created successfully." You land on the list. The new row has status **Draft**, number INV-000NN, Items 1 and the right total.
- **CRT-02 · Default values**
  - Steps: Open New Invoice.
  - Expected: Issue date is today, due date is today + 30 days, tax rate 0, customer empty, no line items ("No line items yet").
- **CRT-03 · Customer dropdown**
  - Steps: Open the customer dropdown and type part of a name in lower case.
  - Expected: Customers with a company name show the company name; others show the person's name. Filtering ignores case.
- **CRT-04 · Save & Continue on a new invoice** (see Known issues)
  - Steps: Fill a valid new invoice and click Save & Continue.
  - Expected: Success toast, the URL changes to /invoices/{id}, and the same invoice opens for editing with the title INV-000NN.

#### Validation

- **CRT-05 · No customer selected**
  - Steps: Leave the customer empty, add a valid line and save.
  - Expected: Error toast "Customer is required." Nothing is saved.
- **CRT-06 · Due date before issue date**
  - Steps: Try to pick a due date earlier than the issue date. Then try typing one in manually.
  - Expected: The picker blocks earlier dates. If a typed date gets through, saving shows "Due date must be on or after the issue date."
- **CRT-07 · Move issue date past due date**
  - Steps: Set the issue date to a day after the current due date.
  - Expected: The due date moves automatically to the new issue date + 30 days.
- **CRT-08 · Tax rate out of range**
  - Steps: Type -5, then 150, into Tax Rate.
  - Expected: The field keeps the value between 0 and 100. If a value outside that range reaches the server: "Tax rate must be between 0 and 100."

#### Edge cases

- **CRT-09 · Due date equal to issue date**
  - Steps: Set both dates to the same day and save.
  - Expected: Saves fine.
- **CRT-10 · Tax rate 0 and 100**
  - Steps: Save one invoice with tax 0 and another with tax 100.
  - Expected: With 0% tax, Tax is 0.00 and Total equals Subtotal. With 100% tax, Total is double the Subtotal.
- **CRT-11 · Decimal tax rate**
  - Steps: Set tax 7.25 on a subtotal of 100.00.
  - Expected: Tax 7.25, total 107.25. The label may show "Tax (7.3%)" because it displays one decimal. The calculation must still use 7.25.
- **CRT-12 · Invoice with no line items**
  - Steps: Pick a customer, add no lines, save.
  - Expected: Saves as Draft with Items 0 and Total 0.00.
- **CRT-13 · Clear a date field** (see Known issues)
  - Steps: Delete the text in Issue date (leave it empty), then save.
  - Expected: Check what gets saved.
- **CRT-14 · Cancel a new invoice**
  - Steps: Fill a few fields, then click Cancel.
  - Expected: "You have unsaved changes" dialog. Stay keeps the form; Leave returns to the list and nothing is saved.

### Line items & totals

The Line Items grid on the invoice form, and the Subtotal / Tax / Total card. Totals update while you type.

#### Happy path

- **LIN-01 · Add a line item**
  - Steps: Click Add Line Item, fill Description, Qty, Unit Price and Discount %, then click ✓.
  - Expected: The row is saved in the grid, Line Total shows, and Subtotal, Tax and Total update.
- **LIN-02 · Check the math**
  - Steps: Line: qty 2 × 50.00, discount 10%. Tax rate 11%.
  - Expected: Line total 90.00, Subtotal 90.00, Tax 9.90, Total **99.90**. The same total appears on the list after saving.
- **LIN-03 · Several lines**
  - Steps: Add 3 lines with different values.
  - Expected: Subtotal is exactly the sum of the three line totals.
- **LIN-04 · Edit a line**
  - Steps: Click the pencil on a row, change Qty, click ✓.
  - Expected: Row and totals update.
- **LIN-05 · Cancel a line edit**
  - Steps: Click the pencil, change values, click ✕.
  - Expected: The row goes back to its old values.
- **LIN-06 · Delete a line**
  - Steps: Click the trash icon on a row.
  - Expected: Row removed straight away (no confirmation) and totals update. The change is saved only when you save the invoice.

#### Validation

- **LIN-07 · Empty description**
  - Steps: Add a line with no description, click ✓, then save the invoice.
  - Expected: Error toast "Description is required." Nothing saved.
- **LIN-08 · Description with only spaces**
  - Steps: Enter "   " as the description and save.
  - Expected: Spaces are trimmed, so the same "Description is required." message appears.
- **LIN-09 · Description over 200 characters**
  - Steps: Paste 201 characters and save.
  - Expected: "Description cannot exceed 200 characters." Exactly 200 characters saves.
- **LIN-10 · Quantity 0**
  - Steps: Try to set Qty to 0.
  - Expected: The field doesn't allow less than 0.01. If 0 reaches the server: "Quantity must be greater than 0."
- **LIN-11 · Quantity with 3 decimals**
  - Steps: Qty 1.555, then save.
  - Expected: "Quantity cannot have more than 2 decimal places." (Check whether the field rounds it first.)
- **LIN-12 · Unit price with 3 decimals**
  - Steps: Unit price 9.999, then save.
  - Expected: "Unit price cannot have more than 2 decimal places."
- **LIN-13 · Negative unit price**
  - Steps: Type -10 in Unit Price.
  - Expected: Blocked by the field (min 0), or "Unit price cannot be negative."
- **LIN-14 · Discount over 100**
  - Steps: Type 150 in Discount %.
  - Expected: Kept between 0 and 100, or "Discount must be between 0 and 100."
- **LIN-15 · Two invalid lines**
  - Steps: Make line 1 and line 3 both invalid, then save.
  - Expected: Only the first line's error shows. After fixing it, the next save shows line 3's error.
  - Note: The validator stops at the first bad line and joins messages into one toast. Decide whether you want all errors at once.

#### Edge cases

- **LIN-16 · Free item**
  - Steps: Unit price 0.
  - Expected: Allowed. Line total 0.00.
- **LIN-17 · 100% discount**
  - Steps: Discount 100 on a 50.00 line.
  - Expected: Line total 0.00.
- **LIN-18 · Rounding: form vs list** (see Known issues)
  - Steps: Add 3 lines, each qty 1 × 0.05 with 10% discount, tax 0. Save, then compare the total on the form and on the list.
  - Expected: Both show **0.12** (each line is 0.04).
- **LIN-19 · Rounding with odd numbers**
  - Steps: Qty 3 × 3.33, discount 12.5%, tax 11%.
  - Expected: Line 8.74, Tax 0.96, Total 9.70 on the form, the list and after reopening.
- **LIN-20 · Large amounts**
  - Steps: Qty 10,000 × 99,999.99, discount 0, tax 11%.
  - Expected: Subtotal **999,999,900.00**, Tax **109,999,989.00**, Total **1,109,999,889.00** on the form, the list and after reopening.
  - Note: SQLite stores Quantity, UnitPrice, Discount and TaxRate as double. Check that very large values don't lose cents.
- **LIN-21 · Large amounts with cents** (see Known issues)
  - Steps: Qty 12,345.67 × 98,765.43, discount 7.5%, tax 11%.
  - Expected: Discount 91,449,405.46, Subtotal 1,127,876,000.73, Tax 124,066,360.08, Total **1,251,942,360.81** on the form, the list and after reopening.
- **LIN-22 · Add Line Item twice**
  - Steps: Click Add Line Item, then click it again before confirming the first row.
  - Expected: The second click does nothing. Only one new row is open.
- **LIN-23 · Edit a row while a new row is open**
  - Steps: Click Add Line Item, then click the pencil on another row.
  - Expected: The unconfirmed new row disappears and the other row opens for editing.
- **LIN-24 · Arabic and special characters**
  - Steps: Descriptions like محمد, José, O'Brien & Co. `<i>test</i>`
  - Expected: Saved and shown exactly as typed. No HTML is rendered.

### Edit & view invoice

Opening an invoice from the list (/invoices/{id}). Only Draft invoices can be edited.

#### Happy path

- **EDT-01 · Open a draft**
  - Steps: From the list, open the row menu (⋮) of a Draft and choose Edit.
  - Expected: Form opens with the title INV-000NN, the Draft badge, all fields editable, and Cancel / Save & Continue / Save Changes buttons.
- **EDT-02 · Edit and save**
  - Steps: Change the customer, dates, tax and a line, then click Save Changes.
  - Expected: "Invoice updated successfully.", back to the list, row shows the new values. Reopen to confirm.
- **EDT-03 · Save & Continue on a draft**
  - Steps: Change something and click Save & Continue.
  - Expected: Success toast, you stay on the page, and the data reloads with the saved values.
- **EDT-04 · Leave with unsaved changes**
  - Steps: Change a field, then click the back arrow or Cancel.
  - Expected: "You have unsaved changes" dialog. Stay keeps your edits; Leave discards them.

#### Validation

- **EDT-05 · Open a non-draft invoice**
  - Steps: Open a Sent, a Paid and a Cancelled invoice (the menu item is called View).
  - Expected: Read-only form: fields disabled, no line item buttons, only Back to List.

#### Edge cases

- **EDT-06 · Leave without changes**
  - Steps: Open a draft, change nothing, click back.
  - Expected: No unsaved-changes dialog.
- **EDT-07 · Refresh or close with unsaved changes**
  - Steps: Change a field and press F5 or close the tab.
  - Expected: The browser asks whether you want to leave.
- **EDT-08 · No prompt after saving**
  - Steps: Save with Save & Continue, then click back.
  - Expected: No unsaved-changes dialog.
- **EDT-09 · Invoice that doesn't exist**
  - Steps: Go to /invoices/99999.
  - Expected: "Invoice not found" with a Back to Invoices button.
- **EDT-10 · Invalid id in URL**
  - Steps: Go to /invoices/abc and /invoices/-1.
  - Expected: abc shows the app's Not Found page (the route only accepts numbers). -1 shows "Invoice not found".

### Status changes

Row menu (⋮) → Change Status. Allowed moves: Draft → Sent or Cancelled; Sent → Paid or Cancelled. Paid and Cancelled are final.

#### Happy path

- **STS-01 · Draft → Sent**
  - Steps: On a draft with at least one line item, choose Change Status → Sent → Save.
  - Expected: Toast "Invoice INV-… is now Sent." and the badge turns to Sent.
- **STS-02 · Sent → Paid**
  - Steps: Change Status → Paid → Save.
  - Expected: A confirm dialog says it can't be undone. Confirm, and the status becomes Paid.
- **STS-03 · Draft → Cancelled**
  - Steps: Change Status → Cancelled → Save → Confirm.
  - Expected: Status becomes Cancelled.
- **STS-04 · Sent → Cancelled**
  - Steps: Change Status → Cancelled → Save → Confirm.
  - Expected: Status becomes Cancelled.

#### Validation

- **STS-05 · Offered options per status**
  - Steps: Open Change Status on a Draft, then on a Sent invoice. Open the menu of a Paid and a Cancelled invoice.
  - Expected: Draft offers Sent and Cancelled. Sent offers Paid and Cancelled. Paid and Cancelled have no Change Status item, only View.
- **STS-06 · Dialog without a choice**
  - Steps: Open Change Status and don't pick anything.
  - Expected: Save is disabled. Cancel, or a click outside the dialog, closes it with no change.
- **STS-07 · Cancel the confirm step**
  - Steps: Choose Paid, click Save, then Cancel in the confirm dialog.
  - Expected: Nothing changes.

#### Edge cases

- **STS-08 · Overdue highlight**
  - Steps: Look at a Sent invoice whose due date is in the past.
  - Expected: The due date is shown in red with the tooltip "Overdue".
- **STS-09 · Not overdue**
  - Steps: Look at Draft, Paid and Cancelled invoices with past due dates, and a Sent invoice due today.
  - Expected: None of them are red.
- **STS-10 · Overdue around midnight** (see Known issues)
  - Steps: Between 00:00 and 03:00 Beirut time, check a Sent invoice that was due yesterday.
  - Expected: It should be red.
- **STS-11 · After sending**
  - Steps: Mark a draft as Sent, then open its row menu.
  - Expected: The menu shows View (not Edit) and no Delete. The form opens read-only.

### Delete invoice

Row menu (⋮) → Delete. Only Draft invoices can be deleted.

#### Happy path

- **DEL-01 · Delete a draft**
  - Steps: Choose Delete on a draft, then click Delete in the confirm dialog.
  - Expected: Confirm text "Delete INV-…? This cannot be undone.", then toast "Invoice Deleted". The row disappears and the total count drops by 1.
- **DEL-02 · Cancel the delete**
  - Steps: Choose Delete, then Cancel.
  - Expected: Nothing is deleted.

#### Validation

- **DEL-03 · Non-draft invoices**
  - Steps: Open the row menu of a Sent, a Paid and a Cancelled invoice.
  - Expected: No Delete option.

#### Edge cases

- **DEL-04 · Line items removed too**
  - Steps: Delete a draft that had line items.
  - Expected: The invoice and its lines are gone (cascade delete). No error.
- **DEL-05 · Numbers are not reused**
  - Steps: Delete the newest invoice, then create a new one.
  - Expected: The new invoice gets a new number; it doesn't take the deleted one's number.

### Invoice list: search, filter, sort, paging

The Invoices page (/invoices).

#### Happy path

- **LST-01 · Default view**
  - Steps: Open Invoices.
  - Expected: Newest issue date first, 10 rows per page, summary "Showing 1 to 10 of N invoices".
- **LST-02 · Search by customer name**
  - Steps: Type part of a customer's name, in any case.
  - Expected: Only that customer's invoices show.
- **LST-03 · Search by company name**
  - Steps: Type part of a company name.
  - Expected: Matching invoices show; the Customer column shows the company name.
- **LST-04 · Search by invoice number**
  - Steps: Try INV-00012, inv-12 and 12.
  - Expected: All three find invoice INV-00012.
- **LST-05 · Filter by status**
  - Steps: Pick each status in the dropdown, then clear it with the ×.
  - Expected: Only that status shows; clearing shows everything again.

#### Validation

- **LST-06 · No results**
  - Steps: Search for zzzz.
  - Expected: "No invoices found" with the hint "Try adjusting your search or filter criteria."
- **LST-07 · Empty database**
  - Steps: Test with no invoices at all.
  - Expected: "No invoices found" with the hint "Create your first invoice to get started."
- **LST-08 · Search and status together**
  - Steps: Search a customer and filter by Paid.
  - Expected: Only that customer's Paid invoices.

#### Edge cases

- **LST-09 · Spaces around the search**
  - Steps: Type "  inv-12  ".
  - Expected: Same result as without the spaces.
- **LST-10 · Number that is also in a name**
  - Steps: Search 12 when a customer is called "Store 12".
  - Expected: Shows invoice 12 and all of Store 12's invoices.
- **LST-11 · Special characters**
  - Steps: Search ' then % then _.
  - Expected: No error. Only real matches show (% and _ don't act as wildcards).
- **LST-12 · Sort every column**
  - Steps: Click each header twice: Number, Customer, Issue Date, Due Date, Status, Items, Total.
  - Expected: Ascending, then descending, and correct each time.
  - Note: Status is stored as text, so it sorts alphabetically: Cancelled, Draft, Paid, Sent (not in workflow order). Decide if that's acceptable.
- **LST-13 · Page size**
  - Steps: Switch to 20 and 50 rows per page.
  - Expected: Row count and the summary text update.
- **LST-14 · Search resets the page**
  - Steps: Go to page 2, then type a search.
  - Expected: Jumps back to page 1 with the filtered results.
- **LST-15 · Fast typing**
  - Steps: Type a long search quickly.
  - Expected: The grid reloads once, shortly after you stop typing, and matches the final text.

### Dashboard cards

The status cards on the home page (/).

#### Happy path

- **DSH-01 · Card numbers**
  - Steps: Compare Total, Draft, Sent, Paid and Cancelled with the list filtered by each status.
  - Expected: Every card matches its filtered list count.
- **DSH-02 · Total equals the sum**
  - Steps: Add up Draft + Sent + Paid + Cancelled.
  - Expected: Equals the Total card.

#### Validation

- **DSH-03 · Cards update**
  - Steps: Create an invoice, send one, delete a draft, then go back to the dashboard.
  - Expected: All the numbers reflect the changes.

### Access & connection

All invoice pages require login.

#### Happy path

- **ACC-01 · Logged out**
  - Steps: Log out, then open /, /invoices and /invoices/new directly.
  - Expected: Redirected to the login page each time.

#### Edge cases

- **ACC-02 · Lost connection**
  - Steps: With a form open, stop the app (or turn off the network) for a few seconds, then bring it back.
  - Expected: The reconnect dialog appears, then the page reconnects. Note whether your unsaved changes survive.

### Concurrency & failures

Use two tabs (A and B) with the same invoice open. These are the cases most likely to find real bugs.

#### Complex

- **CON-01 · Two tabs edit the same draft**
  - Steps: In A and B, change different fields on the same draft. Save A, then save B.
  - Expected: B's save wins (there is no "someone else changed this" check on the header). Reopen: line items are not duplicated.
- **CON-02 · Edit after the other tab sent it**
  - Steps: In A, mark the draft as Sent. In B, change the draft form and save.
  - Expected: "Only draft invoices can be edited."
- **CON-03 · Deleted in the other tab**
  - Steps: In A, delete a draft. In B, try Change Status from the list, then save the open form.
  - Expected: Both show "Invoice not found."
- **CON-04 · Stale list: delete**
  - Steps: In A, send a draft. In B's list (still showing Draft), choose Delete.
  - Expected: "Only draft invoices can be deleted." The invoice is not deleted.
- **CON-05 · Stale list: status**
  - Steps: In A, mark an invoice Paid. In B's list (still showing Sent), choose Cancelled.
  - Expected: "Cannot transition from Paid to Cancelled."
- **CON-06 · Double-click Save**
  - Steps: On a valid new invoice, double-click Save Invoice quickly.
  - Expected: Exactly one invoice is created.
- **CON-07 · Save & Continue twice on a new invoice** (see Known issues)
  - Steps: Click Save & Continue on a new invoice, then Save & Continue again on the page you land on.
  - Expected: Still one invoice; the second save updates it.
- **CON-08 · Database error while saving**
  - Steps: Lock or remove the SQLite file (or break the connection string) and save an invoice.
  - Expected: "Failed to save invoice. Please try again." No invoice is saved without its lines (the transaction rolls back).
- **CON-09 · Customer added while the form is open** (see Known issues)
  - Steps: With New Invoice open, add a customer (another tab or the database), then open the customer dropdown.
  - Expected: The new customer appears.
- **CON-10 · Years and dates across boundaries**
  - Steps: Create invoices dated 31 Dec and 1 Jan, then sort by Issue Date and Due Date.
  - Expected: Sorted in correct date order across the year change.
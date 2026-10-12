import { test, expect } from '@playwright/test';

test('Create ticket, update status and verify persistence', async ({ page }) => {
    await page.goto('http://localhost:5173');

    const issue = `Playwright E2E Test ${Date.now()}`;

    // 1. Create a ticket
    await page.getByPlaceholder('Describe the issue...').fill(issue);

    await page.getByRole('button', { name: 'Create Ticket' }).click();

    // 2. Verify ticket appears
    const ticket = page.getByText(issue, { exact: true });
    await expect(ticket).toBeVisible();

    // 3. Open ticket details
    await ticket.click();

    await expect(
        page.getByText('Ticket Details')
    ).toBeVisible();

    // 4. Update status
    const statusDropdown = page.getByRole('combobox').last();

    await statusDropdown.selectOption({ label: 'In Progress' });

    await expect(statusDropdown).toHaveValue('In Progress');

    // 5. Reload dashboard
    await page.reload();

    // 6. Find the same ticket
    await page.getByText(issue, { exact: true }).click();

    // 7. Verify status persisted
    const statusAfterReload = page.getByRole('combobox').last();

    await expect(statusAfterReload).toHaveValue('In Progress');
});
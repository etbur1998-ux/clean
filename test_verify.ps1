# Backfill existing Paid transport requests into monthly_receipts
# Get all paid requests
$r = Invoke-WebRequest -Uri 'http://localhost:5000/api/transport/requests?role=staff' -UseBasicParsing
$data = $r.Content | ConvertFrom-Json
$paid = $data | Where-Object { $_.status -in @('Paid','StaffApproved','ReceiptVerified') }

Write-Host "=== Paid requests to backfill: $($paid.Count) ==="
foreach ($item in $paid) {
    Write-Host "  $($item.request_number) | status=$($item.status) | cost=$($item.transport_cost) | mahberat=$($item.receipt_mahberat_name)"
}

Write-Host ""
Write-Host "=== Current Monthly Receipts ==="
# Check what's in monthly_receipts via the page
$page = Invoke-WebRequest -Uri 'http://localhost:5000/Dashboard/WeredaMahberat/MonthlyReceipt' -UseBasicParsing
if ($page.Content -match 'No completed bills') {
    Write-Host "EMPTY - No bills shown yet"
} elseif ($page.Content -match 'Completed Bills') {
    $matches2 = [regex]::Matches($page.Content, 'TR-\d{8}-\w+')
    Write-Host "Found $($matches2.Count) bill(s):"
    foreach ($m in $matches2) { Write-Host "  $($m.Value)" }
}

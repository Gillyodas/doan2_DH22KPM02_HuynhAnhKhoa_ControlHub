# AuditAI V3.0 — Hướng dẫn kiểm thử cho Claude Code

## Mục tiêu
Gọi endpoint `POST /api/audit/v3/investigate` cho 5 session log thực tế, đo thời gian phản hồi, ghi kết quả vào file JSON để tính accuracy.

---

## Thông tin endpoint

- **URL**: `https://localhost:7110/api/audit/v3/investigate`
- **Method**: POST
- **Auth**: Bearer token (JWT) — xem bước 1
- **Body**:
```json
{
  "query": "<câu hỏi điều tra>",
  "correlationId": "<optional — connection ID từ log>"
}
```
- **Response**:
```json
{
  "query": "...",
  "answer": "...",
  "plan": ["step1", "step2", ...],
  "executionResults": [...],
  "verificationPassed": true/false,
  "iterations": 2,
  "confidence": 0.85,
  "error": null,
  "version": "V3.0"
}
```

---

## Lưu ý quan trọng

1. **SSL**: API dùng `https://localhost:7110` với self-signed cert — cần bỏ qua SSL verify (`--insecure` với curl, hoặc `verify=False` với Python requests)
2. **Timeout**: Mỗi request mất 3–6 phút do LLM xử lý. Đặt timeout tối thiểu **600 giây**
3. **Đo latency**: Đo từ lúc gửi request đến lúc nhận response hoàn chỉnh (tính bằng giây)
4. **Ghi VRAM**: Chạy `nvidia-smi --query-gpu=memory.used --format=csv,noheader,nounits` mỗi 2 giây trong khi request đang chạy — lấy giá trị max
5. **Lấy token**: Gọi `POST /api/auth/auth/signin` trước, lấy `accessToken` trong response

---

## Bước 1 — Lấy Bearer Token

```python
import requests

signin_url = "https://localhost:7110/api/auth/auth/signin"
payload = {
    "value": "gillyodaswork@gmail.com",
    "password": "Admin@123"
}
resp = requests.post(signin_url, json=payload, verify=False)
token = resp.json()["accessToken"]
print(f"Token: {token[:40]}...")
```

---

## Bước 2 — Chạy 5 test case

Gọi lần lượt từng test case, **không chạy song song** (GPU không đủ VRAM cho 2 request cùng lúc).

### Test case định nghĩa

```python
TEST_CASES = [
    {
        "session_id": "session-001",
        "ground_truth_code": "rate_limit_exceeded",
        "ground_truth_label": "Lỗi — Đăng nhập vượt giới hạn tốc độ",
        "query": "Phân tích log trong khoảng thời gian 17:04:51 đến 17:04:59 ngày 2026-03-07. Xác định nguyên nhân gốc rễ của sự cố (nếu có) và đánh giá mức độ nghiêm trọng.",
        "correlationId": "0HNJSDE8UT4G9"
    },
    {
        "session_id": "session-004",
        "ground_truth_code": "token_expired",
        "ground_truth_label": "Lỗi xác thực — Token truy cập hết hạn",
        "query": "Phân tích log trong khoảng thời gian 16:21:45 đến 16:21:52 ngày 2026-03-08. Xác định nguyên nhân gốc rễ của sự cố (nếu có) và đánh giá mức độ nghiêm trọng.",
        "correlationId": "0HNJT5R3F44QF"
    },
    {
        "session_id": "session-002",
        "ground_truth_code": "token_expired_then_refreshed",
        "ground_truth_label": "Lỗi xác thực — Token hết hạn (đã làm mới thành công)",
        "query": "Phân tích log trong khoảng thời gian 17:07:19 đến 17:07:53 ngày 2026-03-07. Xác định nguyên nhân gốc rễ của sự cố (nếu có) và đánh giá mức độ nghiêm trọng.",
        "correlationId": "0HNJSDFU63NU5"
    },
    {
        "session_id": "session-014",
        "ground_truth_code": "token_invalid",
        "ground_truth_label": "Lỗi xác thực — Token không hợp lệ",
        "query": "Phân tích log trong khoảng thời gian 07:55:26 đến 07:55:42 ngày 2026-03-10. Xác định nguyên nhân gốc rễ của sự cố (nếu có) và đánh giá mức độ nghiêm trọng.",
        "correlationId": "0HNJUF7Q7UDMR"
    },
    {
        "session_id": "session-010",
        "ground_truth_code": "normal_signin",
        "ground_truth_label": "Bình thường — Đăng nhập thành công",
        "query": "Phân tích log trong khoảng thời gian 07:18:05 đến 07:19:05 ngày 2026-03-10. Xác định nguyên nhân gốc rễ của sự cố (nếu có) và đánh giá mức độ nghiêm trọng.",
        "correlationId": "0HNJUEKLRISL8"
    }
]
```

---

## Bước 3 — Script hoàn chỉnh

Tạo file `run_auditai_test.py` với nội dung sau và chạy bằng Claude Code:

```python
import requests
import json
import time
import subprocess
import threading
import urllib3
from datetime import datetime

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

BASE_URL = "https://localhost:7110"
TIMEOUT = 600  # 10 phút timeout

# ─── Lấy token ───────────────────────────────────────────────────
def get_token():
    resp = requests.post(
        f"{BASE_URL}/api/auth/auth/signin",
        json={"value": "gillyodaswork@gmail.com", "password": "Admin@123"},
        verify=False,
        timeout=30
    )
    resp.raise_for_status()
    return resp.json()["accessToken"]

# ─── Theo dõi VRAM ───────────────────────────────────────────────
vram_readings = []
vram_monitoring = False

def monitor_vram():
    global vram_readings, vram_monitoring
    while vram_monitoring:
        try:
            result = subprocess.run(
                ["nvidia-smi", "--query-gpu=memory.used", "--format=csv,noheader,nounits"],
                capture_output=True, text=True, timeout=5
            )
            val = int(result.stdout.strip())
            vram_readings.append(val)
        except Exception:
            pass
        time.sleep(2)

# ─── Test cases ───────────────────────────────────────────────────
TEST_CASES = [
    {
        "session_id": "session-001",
        "ground_truth_code": "rate_limit_exceeded",
        "ground_truth_label": "Lỗi — Đăng nhập vượt giới hạn tốc độ",
        "query": "Phân tích log trong khoảng thời gian 17:04:51 đến 17:04:59 ngày 2026-03-07. Xác định nguyên nhân gốc rễ của sự cố (nếu có) và đánh giá mức độ nghiêm trọng.",
        "correlationId": "0HNJSDE8UT4G9"
    },
    {
        "session_id": "session-004",
        "ground_truth_code": "token_expired",
        "ground_truth_label": "Lỗi xác thực — Token truy cập hết hạn",
        "query": "Phân tích log trong khoảng thời gian 16:21:45 đến 16:21:52 ngày 2026-03-08. Xác định nguyên nhân gốc rễ của sự cố (nếu có) và đánh giá mức độ nghiêm trọng.",
        "correlationId": "0HNJT5R3F44QF"
    },
    {
        "session_id": "session-002",
        "ground_truth_code": "token_expired_then_refreshed",
        "ground_truth_label": "Lỗi xác thực — Token hết hạn (đã làm mới thành công)",
        "query": "Phân tích log trong khoảng thời gian 17:07:19 đến 17:07:53 ngày 2026-03-07. Xác định nguyên nhân gốc rễ của sự cố (nếu có) và đánh giá mức độ nghiêm trọng.",
        "correlationId": "0HNJSDFU63NU5"
    },
    {
        "session_id": "session-014",
        "ground_truth_code": "token_invalid",
        "ground_truth_label": "Lỗi xác thực — Token không hợp lệ",
        "query": "Phân tích log trong khoảng thời gian 07:55:26 đến 07:55:42 ngày 2026-03-10. Xác định nguyên nhân gốc rễ của sự cố (nếu có) và đánh giá mức độ nghiêm trọng.",
        "correlationId": "0HNJUF7Q7UDMR"
    },
    {
        "session_id": "session-010",
        "ground_truth_code": "normal_signin",
        "ground_truth_label": "Bình thường — Đăng nhập thành công",
        "query": "Phân tích log trong khoảng thời gian 07:18:05 đến 07:19:05 ngày 2026-03-10. Xác định nguyên nhân gốc rễ của sự cố (nếu có) và đánh giá mức độ nghiêm trọng.",
        "correlationId": "0HNJUEKLRISL8"
    }
]

# ─── Chạy test ────────────────────────────────────────────────────
def run_test(tc, token):
    global vram_readings, vram_monitoring

    print(f"\n{'='*60}")
    print(f"[{tc['session_id']}] Ground truth: {tc['ground_truth_label']}")
    print(f"Query: {tc['query'][:80]}...")

    # Start VRAM monitor
    vram_readings = []
    vram_monitoring = True
    vram_thread = threading.Thread(target=monitor_vram, daemon=True)
    vram_thread.start()

    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    body = {"query": tc["query"], "correlationId": tc["correlationId"]}

    start_time = time.time()
    error_msg = None
    response_data = {}

    try:
        resp = requests.post(
            f"{BASE_URL}/api/audit/v3/investigate",
            json=body,
            headers=headers,
            verify=False,
            timeout=TIMEOUT
        )
        resp.raise_for_status()
        response_data = resp.json()
    except requests.exceptions.Timeout:
        error_msg = "TIMEOUT after 600s"
    except Exception as e:
        error_msg = str(e)

    latency_s = round(time.time() - start_time, 2)

    # Stop VRAM monitor
    vram_monitoring = False
    vram_thread.join(timeout=3)
    vram_peak = max(vram_readings) if vram_readings else None

    result = {
        "session_id": tc["session_id"],
        "ground_truth_code": tc["ground_truth_code"],
        "ground_truth_label": tc["ground_truth_label"],
        "latency_seconds": latency_s,
        "vram_peak_mb": vram_peak,
        "ai_answer": response_data.get("answer", ""),
        "ai_plan": response_data.get("plan", []),
        "verification_passed": response_data.get("verificationPassed", False),
        "confidence": response_data.get("confidence", 0),
        "iterations": response_data.get("iterations", 0),
        "ai_error": response_data.get("error") or error_msg,
        "request_error": error_msg,
        "timestamp": datetime.now().isoformat()
    }

    print(f"Latency: {latency_s}s | VRAM peak: {vram_peak} MB | Confidence: {result['confidence']:.2f}")
    print(f"Answer preview: {result['ai_answer'][:200]}...")

    return result

# ─── Main ─────────────────────────────────────────────────────────
def main():
    print("Getting token...")
    token = get_token()
    print("Token acquired.")

    results = []
    for tc in TEST_CASES:
        result = run_test(tc, token)
        results.append(result)
        # Save after each test (in case of crash)
        with open("auditai_test_results.json", "w", encoding="utf-8") as f:
            json.dump(results, f, ensure_ascii=False, indent=2)
        print(f"Saved result for {tc['session_id']}")
        # Wait 10s between requests to let GPU cool down
        if tc != TEST_CASES[-1]:
            print("Waiting 10s before next request...")
            time.sleep(10)

    print(f"\n{'='*60}")
    print(f"All tests completed. Results saved to auditai_test_results.json")
    print(f"Total sessions tested: {len(results)}")

if __name__ == "__main__":
    main()
```

---

## Bước 4 — Chạy script

Trong Claude Code terminal, tại thư mục project:

```bash
cd E:\Project\ControlHub
pip install requests
python run_auditai_test.py
```

**Đảm bảo trước khi chạy:**
- ControlHub API đang chạy tại `https://localhost:7110`
- Ollama đang chạy (`ollama serve`)
- Qdrant container đang chạy
- File log thực tế nằm trong thư mục `Logs/` của project

---

## Bước 5 — Gửi kết quả

Sau khi script chạy xong, file `auditai_test_results.json` sẽ được tạo. Gửi file đó vào chat để mình tính accuracy và viết phần 5.5.2 + 5.5.3.

---

## Cấu trúc file kết quả mong đợi

```json
[
  {
    "session_id": "session-001",
    "ground_truth_code": "rate_limit_exceeded",
    "ground_truth_label": "Lỗi — Đăng nhập vượt giới hạn tốc độ",
    "latency_seconds": 245.3,
    "vram_peak_mb": 3842,
    "ai_answer": "Phân tích cho thấy...",
    "ai_plan": ["Hypothesis 1...", "Hypothesis 2..."],
    "verification_passed": true,
    "confidence": 0.82,
    "iterations": 3,
    "ai_error": null,
    "request_error": null,
    "timestamp": "2026-03-17T..."
  },
  ...
]
```

---

## Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân | Xử lý |
|---|---|---|
| `ConnectionRefused` | API chưa chạy | Khởi động ControlHub API |
| `Timeout after 600s` | LLM quá chậm | Ghi lại, bỏ qua, chạy test tiếp |
| `401 Unauthorized` | Token hết hạn | Gọi lại hàm `get_token()` |
| `400 Bad Request` | V3 chưa enable | Kiểm tra `AuditAI:Version = V3.0` trong appsettings |
| `VRAM = None` | nvidia-smi không có | Bỏ qua, điền thủ công sau |

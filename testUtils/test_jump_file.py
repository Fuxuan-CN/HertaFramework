# 文件名：test_file_service.py
import requests
import pytest

# API 基础 URL
BASE_URL = "http://localhost:5000/api/file"

# 测试用例
@pytest.mark.parametrize("user_id,filename,expected_status,description", [
    ("1", "test.txt", {200, 404}, "正常文件路径"),
    ("1", "../../../../etc/passwd", {403, 404}, "恶意路径（尝试访问系统文件）"),
    ("1", "..\\..\\..\\Windows\\System32\\drivers\\etc\\hosts", {403, 404}, "恶意路径（尝试访问 Windows 系统文件）"),
    ("1", "我.png", {200, 404}, "包含中文字符的文件名"),
    ("1", "test/../../etc/passwd", {403, 404}, "恶意路径（尝试跳转目录）"),
])

def test_file_service(user_id, filename, expected_status, description):
    url = f"{BASE_URL}/{user_id}/{filename}"
    response = requests.get(url)

    assert response.status_code in expected_status, \
        f"测试失败: {description}\n" \
        f"请求路径: {url}\n" \
        f"预期状态码: {expected_status}\n" \
        f"实际状态码: {response.status_code}\n" \
        f"响应内容: {response.text}"

    print(f"测试场景: {description}")
    print(f"请求路径: {url}")
    print(f"预期状态码: {expected_status}")
    print(f"实际状态码: {response.status_code}")
    print(f"响应内容: {response.text}")
    print("-" * 50)
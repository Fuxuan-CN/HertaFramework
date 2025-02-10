import requests
import pytest

BASE = "http://127.0.0.1:8000/api"
USER = f"{BASE}/user"
REG = f"{USER}/register"
LOGIN = f"{USER}/login"
DELETE = f"{USER}/delete"
CHANGE = f"{USER}/change"
UPDATE_INFO = f"{USER}/update/info"
GET_INFO = f"{USER}/get/info/{{id}}"

# 注册数据
reg_data = {
    "username": "testUser",
    "password": "password123"
}

# 重复注册数据（用户名已存在）
reg_data_existing = {
    "username": "testUser",
    "password": "password123"
}

# 登录数据
login_data = {
    "username": "testUser",
    "password": "password123"
}

# 错误登录数据
wrong_login_data = {
    "username": "testUser",
    "password": "wrongpassword"
}

# 更改密码数据
change_pwd = {
    "username": "testUser",
    "oldPassword": "password123",
    "newPassword": "password456"
}

# 更新用户信息数据
update_patch = {
    "UserId": 1,
    "updateInfo": {
        "Nickname": "testUser2",
        "Hobbies": ["reading", "swimming"]
    }
}

# 删除用户数据
delete_user = {
    "Username": "testUser",
    "Password": "password456",
    "Reason": "testing"
}

@pytest.fixture
def headers():
    token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyIjoidGVzdFVzZXIiLCJ1c2VySWQiOiIxIiwibmJmIjoxNzM4ODk4ODQ4LCJleHAiOjE3Mzk1MDM2NDgsImlhdCI6MTczODg5ODg0OCwiaXNzIjoiaG9iYnktZ3JvdXAifQ.F907K6OG7fY2uTwNh10GnPBaWML2UK1p6xU-to5w6pI"
    return {"Authorization": f"Bearer {token}"}

def test_register():
    # 测试正常注册
    response = requests.post(REG, json=reg_data)
    assert response.status_code == 200, f"注册失败，状态码：{response.status_code}, 响应内容：{response.text}"

    # 测试重复注册
    response = requests.post(REG, json=reg_data_existing)
    assert response.status_code == 400, f"重复注册未返回400状态码，实际状态码：{response.status_code}, 响应内容：{response.text}"

def test_login():
    # 测试正常登录
    response = requests.post(LOGIN, json=login_data)
    assert response.status_code == 200, f"登录失败，状态码：{response.status_code}, 响应内容：{response.text}"

    # 测试错误密码登录
    response = requests.post(LOGIN, json=wrong_login_data)
    assert response.status_code == 401, f"错误密码登录未返回401状态码，实际状态码：{response.status_code}, 响应内容：{response.text}"

def test_change_password(headers):
    # 测试更改密码
    response = requests.post(CHANGE, json=change_pwd, headers=headers)
    assert response.status_code == 200, f"更改密码失败，状态码：{response.status_code}, 响应内容：{response.text}"

def test_update_user_info(headers):
    # 测试部分更新用户信息
    response = requests.patch(UPDATE_INFO, json=update_patch, headers=headers)
    assert response.status_code == 200, f"更新用户信息失败，状态码：{response.status_code}, 响应内容：{response.text}"

def test_get_user_info():
    # 测试获取用户信息
    response = requests.get(GET_INFO.format(id=1))
    assert response.status_code == 200, f"获取用户信息失败，状态码：{response.status_code}, 响应内容：{response.text}"

def test_delete_user(headers):
    # 测试删除用户
    response = requests.delete(DELETE, json=delete_user, headers=headers)
    assert response.status_code == 200, f"删除用户失败，状态码：{response.status_code}, 响应内容：{response.text}"


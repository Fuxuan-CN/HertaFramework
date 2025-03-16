import requests
import pytest

BASE = "http://127.0.0.1:5000"
USER = f"{BASE}/users"
REG = f"{USER}/register"
LOGIN = f"{USER}/login"
DELETE = f"{USER}/delete"
CHANGE = f"{USER}/change"
UPDATE_INFO = f"{USER}/update/info"
GET_INFO = f"{USER}/get/info/{{id}}"
GROUP_ENDPOINT = f"{BASE}/group"
MEMBER_ENDPOINT = f"{GROUP_ENDPOINT}/member"

ID: int = 4
WRONG_ID: int = ID + 1
GROUP_ID: int = ID

join_group_form = {
    "GroupId": GROUP_ID,
    "UserId": ID,
    "JoinAbout": "I want to join this group"
}

unauthorized_join_group_form = {
    "GroupId": GROUP_ID,
    "UserId": WRONG_ID,
    "JoinAbout": "I want to join this group"
}

unauthorized_leave_group_form = {
    "GroupId": GROUP_ID,
    "UserId": WRONG_ID,
    "Reason": "I don't want to join this group"
}

leave_group_form = {
    "GroupId": GROUP_ID,
    "UserId": ID,
    "Reason": "I don't want to join this group"
}

create_group_form = {
    "OwnerId": ID,
    "GroupName": "testGroup",
    "Description": "this is a test group"
}

unauthorized_create_group_form = {
    "OwnerId": WRONG_ID,
    "GroupName": "testGroup",
    "Description": "this is a test group"
}

update_group_unauthorized_form = {
    "OwnerId": WRONG_ID,
    "Fields": {
        "GroupName": "testGroup2",
        "Description": "this is a test group2"
    }
}

update_group_patch_form = {
    "GroupId": GROUP_ID,
    "OwnerId": ID,
    "Fields": {
        "GroupName": "testGroup2",
        "Description": "this is a test group2"
    }
}

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
    "password": "wrongPassword"
}

wrong_data_body = {
    "username": "testUser",
} # 缺少密码字段

# 更改密码数据
change_pwd = {
    "username": "testUser",
    "oldPassword": "password123",
    "newPassword": "password456"
}

# 更新用户信息数据
update_patch = {
    "UserId": ID,
    "updateInfo": {
        "Nickname": "testUser2",
        "Hobbies": ["reading", "swimming"]
    }
}

update_patch_wrong_id = {
    "UserId": 100,
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

TOKEN: str = ""

@pytest.fixture
def headers():
    return {"Authorization": f"Bearer {TOKEN}"}

def test_register():
    # 测试正常注册
    response = requests.post(REG, json=reg_data)
    assert response.status_code == 200, f"注册失败，状态码：{response.status_code}, 响应内容：{response.text}"
    global TOKEN
    TOKEN = response.json()["token"]

    # 测试重复注册
    response = requests.post(REG, json=reg_data_existing)
    assert response.status_code == 400, f"重复注册未返回400状态码，实际状态码：{response.status_code}, 响应内容：{response.text}"

def test_login():
    # 测试正常登录
    response = requests.post(LOGIN, json=login_data)
    assert response.status_code == 200, f"登录失败，状态码：{response.status_code}, 响应内容：{response.text}"

    # 测试缺少密码字段
    response = requests.post(LOGIN, json=wrong_data_body)
    assert response.status_code == 400, f"缺少密码字段未返回400状态码，实际状态码：{response.status_code}, 响应内容：{response.text}"
    print(response.text)

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

def test_update_user_info_unauthorized_id(headers):
    # 尝试未经授权的更新用户信息
    wrong_resp = requests.patch(UPDATE_INFO, json=update_patch_wrong_id, headers=headers)
    assert wrong_resp.status_code == 403, f"状态码不正确，状态码：{wrong_resp.status_code}, 响应内容：{wrong_resp.text}"

def test_get_user_info():
    # 测试获取用户信息
    response = requests.get(GET_INFO.format(id=ID))
    assert response.status_code == 200, f"获取用户信息失败，状态码：{response.status_code}, 响应内容：{response.text}"

def test_group_creation(headers):
    # 测试创建群组
    response = requests.post(GROUP_ENDPOINT, json=create_group_form, headers=headers)
    assert response.status_code == 200, f"创建群组失败，状态码：{response.status_code}, 响应内容：{response.text}"

def test_try_unauthorized_join_group(headers):
    # 尝试未经授权的加入群组
    response = requests.put(MEMBER_ENDPOINT, json=unauthorized_join_group_form, headers=headers)
    assert response.status_code == 403, f"状态码不正确，状态码：{response.status_code}, 响应内容：{response.text}"

def test_leave_group(headers):
    # 测试退出群组
    response = requests.delete(MEMBER_ENDPOINT, json=leave_group_form, headers=headers)
    assert response.status_code == 200, f"退出群组失败，状态码：{response.status_code}, 响应内容：{response.text}"

def test_try_unauthorized_leave_group(headers):
    # 尝试未经授权的退出群组
    response = requests.delete(MEMBER_ENDPOINT, json=unauthorized_leave_group_form, headers=headers)
    assert response.status_code == 403, f"状态码不正确，状态码：{response.status_code}, 响应内容：{response.text}"

def test_try_unauthorized_group_creation(headers):
    # 尝试未经授权的创建群组
    response = requests.post(GROUP_ENDPOINT, json=unauthorized_create_group_form, headers=headers)
    assert response.status_code == 403, f"状态码不正确，状态码：{response.status_code}, 响应内容：{response.text}"

def test_get_group_info():
    # 测试获取群组信息
    response = requests.get(f"{GROUP_ENDPOINT}/{GROUP_ID}")
    assert response.status_code == 200, f"获取群组信息失败，状态码：{response.status_code}, 响应内容：{response.text}"

def test_update_group_info(headers):
    # 测试更新群组信息
    response = requests.patch(f"{GROUP_ENDPOINT}", json=update_group_patch_form, headers=headers)
    assert response.status_code == 200, f"更新群组信息失败，状态码：{response.status_code}, 响应内容：{response.text}"

def test_try_unauthorized_update_group_info_missing_header():
    # 尝试未提供header的更新群组信息
    response = requests.patch(f"{GROUP_ENDPOINT}", json=update_group_patch_form)
    assert response.status_code == 401, f"状态码不正确，状态码：{response.status_code}, 响应内容：{response.text}"

def test_try_unauthorized_update_group_info(headers):
    # 尝试未经授权的更新群组信息
    response = requests.patch(f"{GROUP_ENDPOINT}", json=update_group_unauthorized_form, headers=headers)
    assert response.status_code == 403, f"状态码不正确，状态码：{response.status_code}, 响应内容：{response.text}"

def test_try_unauthorized_delete_group(headers):
    # 尝试未经授权的删除群组
    response = requests.delete(f"{GROUP_ENDPOINT}?GroupId={GROUP_ID}&UserId={WRONG_ID}", headers=headers)
    assert response.status_code == 403, f"状态码不正确，状态码：{response.status_code}, 响应内容：{response.text}"

def test_delete_group(headers):
    # 测试删除群组
    response = requests.delete(f"{GROUP_ENDPOINT}?GroupId={GROUP_ID}&UserId={ID}", headers=headers)
    assert response.status_code == 200, f"删除群组失败，状态码：{response.status_code}, 响应内容：{response.text}"

# 删除用户放在最后，防止影响其他测试
def test_delete_user(headers):
    # 测试删除用户
    response = requests.delete(DELETE, json=delete_user, headers=headers)
    assert response.status_code == 200, f"删除用户失败，状态码：{response.status_code}, 响应内容：{response.text}"
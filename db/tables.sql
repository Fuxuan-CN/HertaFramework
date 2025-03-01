-- 数据库表单设计 v1.3

-- 创建用户表
CREATE TABLE Users (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Freezed BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- 创建用户信息表
CREATE TABLE UserInfos (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    Nickname VARCHAR(50),
    Hobbies JSON,
    Birthday DATETIME,
    Email VARCHAR(255) UNIQUE,
    PhoneNumber CHAR(20),
    FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB;

-- 创建群组表
CREATE TABLE GroupsTable (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    OwnerId INT NOT NULL,
    GroupName VARCHAR(100) NOT NULL,
    Description TEXT,
    AvatarUrl VARCHAR(255),
    Metadata JSON,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (OwnerId) REFERENCES Users (Id) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB;

-- 创建群组成员表
CREATE TABLE GroupMembers (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    GroupId INT NOT NULL,
    UserId INT NOT NULL,
    RoleIs ENUM('OFFICIAL', 'OWNER', 'ADMIN', 'MEMBER') DEFAULT 'MEMBER',
    JoinedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (GroupId) REFERENCES GroupsTable (Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 创建聊天消息表
CREATE TABLE GroupMessages (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    GroupId INT NOT NULL,
    Content TEXT,
    MessageType ENUM('TEXT', 'IMAGE', 'VIDEO', 'AUDIO') DEFAULT 'TEXT',
    FOREIGN KEY (GroupId) REFERENCES GroupsTable (Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 创建文件表
CREATE TABLE GroupFiles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    GroupId INT NOT NULL,
    AccessValue ENUM('PUBLIC', 'PRIVATE', 'FRIENDS') DEFAULT 'PUBLIC',
    FileId CHAR(36) NOT NULL,
    FileName VARCHAR(255) NOT NULL,
    FilePath VARCHAR(255) NOT NULL,
    FileHash CHAR(64) NOT NULL,
    FileSize INT NOT NULL,
    UploadDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
    FOREIGN KEY (GroupId) REFERENCES GroupsTable (Id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 创建索引
CREATE INDEX idx_username ON Users (Username);
CREATE INDEX idx_group_id_user_id ON GroupMembers (GroupId, UserId);
CREATE INDEX idx_user_id_group_id ON GroupMessages (UserId, GroupId);
CREATE INDEX idx_file_id ON GroupFiles (FileId);

-- 创建触发器
DELIMITER $$
CREATE TRIGGER AfterUserInsert
AFTER INSERT ON Users
FOR EACH ROW
BEGIN
    INSERT INTO UserInfos (UserId, Nickname, Hobbies, Birthday, Email, PhoneNumber)
    VALUES (NEW.Id, NEW.Username, '[]', CURDATE(), CONCAT(NEW.Username, '@example.com'), '1234567890');
END$$
DELIMITER ;

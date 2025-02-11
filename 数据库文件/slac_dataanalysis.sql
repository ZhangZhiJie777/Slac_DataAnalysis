/*
 Navicat Premium Data Transfer

 Source Server         : localhost
 Source Server Type    : MySQL
 Source Server Version : 50739 (5.7.39-log)
 Source Host           : localhost:3306
 Source Schema         : slac_dataanalysis

 Target Server Type    : MySQL
 Target Server Version : 50739 (5.7.39-log)
 File Encoding         : 65001

 Date: 23/01/2025 16:14:56
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for system_config
-- ----------------------------
DROP TABLE IF EXISTS `system_config`;
CREATE TABLE `system_config`  (
  `id` int(11) NOT NULL,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Value` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Description` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `parms_type` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of system_config
-- ----------------------------
INSERT INTO `system_config` VALUES (1, 'isCluster', '0', '是否分布式', '报警分析、统计分析');
INSERT INTO `system_config` VALUES (2, 'CHserver', 'slac1028|10.8.255.231|8123', 'clickhouse：密码|IP| 端口', '报警分析、统计分析');
INSERT INTO `system_config` VALUES (3, 'LineID', '120208', '线体号', '报警分析、统计分析');
INSERT INTO `system_config` VALUES (4, 'companyNum', 'chizhoujz', '数据库库名（公司名称）factory', '报警分析、统计分析');
INSERT INTO `system_config` VALUES (5, 'RedisServer', '127.0.0.1|6379|slac1028', 'Rabbit：IP|端口|密码', '报警分析、统计分析');
INSERT INTO `system_config` VALUES (6, 'device_16bit', '12', '16位设备（设备从10开始）区分哪些设备是按照16位解析', '报警分析');
INSERT INTO `system_config` VALUES (7, 'Conn_battery', 'Database=\'czjz_kanban\';Data Source=\'10.8.255.233\';User Id=\'sa\';Password=\'slac.1028\';charset=\'utf8\';pooling=true;SslMode=None', '看板服务器MySql数据库连接字符串', '报警分析、统计分析');
INSERT INTO `system_config` VALUES (8, 'lastAnalyseTime', '0', '上一次报警分析时间', '报警分析（勿动）');

SET FOREIGN_KEY_CHECKS = 1;

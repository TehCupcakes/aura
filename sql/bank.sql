-- phpMyAdmin SQL Dump
-- version 3.5.1
-- http://www.phpmyadmin.net
--
-- Host: localhost
-- Generation Time: Aug 05, 2014 at 02:34 PM
-- Server version: 5.5.24-log
-- PHP Version: 5.4.3

SET SQL_MODE="NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Database: `aura`
--

-- --------------------------------------------------------

--
-- Table structure for table `banks`
--

CREATE TABLE IF NOT EXISTS `banks` (
  `bankId` bigint(20) NOT NULL AUTO_INCREMENT,
  `accountId` varchar(50) NOT NULL,
  `gold` int(10) unsigned NOT NULL DEFAULT '0',
  `lastOpened` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `lock` varchar(32) NOT NULL,
  PRIMARY KEY (`bankId`),
  KEY `accountId` (`accountId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

-- --------------------------------------------------------

--
-- Table structure for table `bank_accounts`
--

CREATE TABLE IF NOT EXISTS `bank_accounts` (
  `creatureName` varchar(50) NOT NULL,
  `bankId` bigint(20) NOT NULL,
  `assistantId` int(1) NOT NULL DEFAULT '0',
  `width` int(11) NOT NULL DEFAULT '24',
  `height` int(11) NOT NULL DEFAULT '8',
  PRIMARY KEY (`creatureName`),
  KEY `bankId` (`bankId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Table structure for table `bank_items`
--

CREATE TABLE IF NOT EXISTS `bank_items` (
  `entityId` bigint(20) NOT NULL,
  `creatureName` varchar(50) NOT NULL,
  `location` varchar(50) NOT NULL,
  `itemId` int(11) NOT NULL,
  `x` int(11) NOT NULL DEFAULT '0',
  `y` int(11) NOT NULL DEFAULT '0',
  `meta1` varchar(2048) DEFAULT NULL,
  `meta2` varchar(2048) DEFAULT NULL,
  PRIMARY KEY (`entityId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `banks`
--
ALTER TABLE `banks`
  ADD CONSTRAINT `banks_ibfk_1` FOREIGN KEY (`accountId`) REFERENCES `accounts` (`accountId`);

--
-- Constraints for table `bank_accounts`
--
ALTER TABLE `bank_accounts`
  ADD CONSTRAINT `bank_accounts_ibfk_2` FOREIGN KEY (`bankId`) REFERENCES `banks` (`bankId`),
  ADD CONSTRAINT `bank_accounts_ibfk_3` FOREIGN KEY (`creatureName`) REFERENCES `creatures` (`name`);

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;

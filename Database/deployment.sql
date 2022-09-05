CREATE DATABASE  IF NOT EXISTS `playlistchaserdb` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `playlistchaserdb`;
-- MySQL dump 10.13  Distrib 8.0.24, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: playlistchaserdb
-- ------------------------------------------------------
-- Server version	8.0.25

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `playlist`
--

DROP TABLE IF EXISTS `playlist`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `playlist` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `youtubeUrl` varchar(255) NOT NULL,
  `uploaderName` varchar(255) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `playlist`
--

LOCK TABLES `playlist` WRITE;
/*!40000 ALTER TABLE `playlist` DISABLE KEYS */;
INSERT INTO `playlist` VALUES (10,'Asd99_11','','https://www.youtube.com/playlist?list=PLH7ljKWzlpEANOGtf_9BUpFZHwoe1wtOO',''),(13,'Mood: Chill Beats','','https://www.youtube.com/playlist?list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS','xKito Music');
/*!40000 ALTER TABLE `playlist` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `song`
--

DROP TABLE IF EXISTS `song`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `song` (
  `id` int NOT NULL AUTO_INCREMENT,
  `youtubeSongName` varchar(255) NOT NULL,
  `foundOnSpotify` bit(1) NOT NULL DEFAULT b'0',
  `playlistId` int NOT NULL,
  `artistName` varchar(255) DEFAULT NULL,
  `youtubeId` varchar(255) NOT NULL,
  `spotifyId` varchar(255) DEFAULT NULL,
  `songName` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `Song_fk0` (`playlistId`),
  CONSTRAINT `Song_fk0` FOREIGN KEY (`playlistId`) REFERENCES `playlist` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=198 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `song`
--

LOCK TABLES `song` WRITE;
/*!40000 ALTER TABLE `song` DISABLE KEYS */;
INSERT INTO `song` VALUES (165,'Eastern Odyssey - Great Divide',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=dD4Pk9nt-jY&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(166,'Eastern Odyssey - Right of Passage',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=1GKB8qCuvlU&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(167,'Submotion Orchestra - Prism',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=aHhpVE4uALk&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(168,'aKu - Buttery',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=lbtaLv7PRXo&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(169,'TOKi​MONSTA - Bibimbap',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=fYbIwn1gQew&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(170,'BLOOM - Just Lay Down',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=jxChKAcjCM0&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(171,'Thaehan - Hope',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=3K18LTjASLg&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(172,'Fallen Roses ft. B dom - Yours And Nobody Else\'s',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=VlBt1uHHSsY&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(173,'Vexento - Never Letting Go',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=sEh3LhwPjCQ&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(174,'Thaehan - Lost',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=-GQn9sN2Ooc&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(175,'Virtual Riot - Part Of Me',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=Zqc-E9_ZiEE&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(176,'The Last Story - Toberu mono (Thaehan Remix)',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=l_T-l4f2w8g&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(177,'CloZee - Secret Place',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=soLrXM0EQ8c&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(178,'Elènne - Bonana',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=kU6Nh06G8Ic&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(179,'Thaehan - Complicity',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=oaIakzShxCs&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(180,'Anatu - Bleach',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=EqdM24AJb3Q&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(181,'UNILAD Beats ft. Beatowski - My Days',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=WXePdL_XlpE&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(182,'Wisp X - Somewhere I\'d Rather Be',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=V5-AQTPFJSg&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(183,'【Chill】Revive Us - Campfire Anthem',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=TSmSFfvxtDs&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(184,'【Chillstep】Oh Wonder - All We Do (LYAR Remix)',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=i59ONAi-5Uw&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(185,'【Chillstep】SizzleBird - Elixir',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=UJfJ5z73fgA&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(186,'【Chillstep】MYKOOL - Ikiru',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=bfRNYRcQugs&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(187,'【Chill】aKu - Love Shine',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=5amlR2sLJio&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(188,'【Chill】aKu - The Final Blow',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=IRD7WylAfkw&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(189,'【Chill】Ark Patrol - Tokyo (Subranger Remix)',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=quhvZ6KG02M&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(190,'【Chillstep】Soulfy - Skyfall [Free Download]',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=WyqFCvPKmVs&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(191,'TroyBoi - X2C',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=Xpt4Ibs0iWw&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(192,'CloZee - Apsara Calling (David Starfire Remix)',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=Fg9M4KlGjUQ&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(193,'Zack the Lad - Won\'t Stop',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=_n_8QlLOmPE&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(194,'Aire Atlantica - May',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=hhMV22rzPgY&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(195,'【Chill Trap】CloZee - Get Up Now',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=_moJEG6M-Cg&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(196,'【Chill Trap】CloZee - Koto',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=l46XpjRj4AM&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL),(197,'【Drum&Bass】DJ Okawari - Luv Letter (Wisp X Remix) [Free Download]',_binary '\0',13,NULL,'https://www.youtube.com/watch?v=ZWYQU4Py7is&list=PLvlw_ICcAI4ft3mI_sS-XBL92BK_yCvMS',NULL,NULL);
/*!40000 ALTER TABLE `song` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping routines for database 'playlistchaserdb'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2022-05-07 19:56:12

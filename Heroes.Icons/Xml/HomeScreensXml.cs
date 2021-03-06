﻿using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Heroes.Icons.Xml
{
    internal class HomeScreensXml : XmlBase, IHomeScreens
    {
        private HomeScreensXml(string parentFile, string xmlBaseFolder, int currentBuild, bool logger)
            : base(currentBuild, logger)
        {
            XmlParentFile = parentFile;
            XmlBaseFolder = xmlBaseFolder;
            XmlFolder = xmlBaseFolder;
        }

        public List<Tuple<BitmapImage, Color>> HomeScreenBackgrounds { get; private set; } = new List<Tuple<BitmapImage, Color>>();

        public static HomeScreensXml Initialize(string parentFile, string xmlFolder, int currentBuild, bool logger)
        {
            HomeScreensXml xml = new HomeScreensXml(parentFile, xmlFolder, currentBuild, logger);
            xml.Parse();
            return xml;
        }

        public List<Tuple<BitmapImage, Color>> GetListOfHomeScreens()
        {
            return HomeScreenBackgrounds;
        }

        protected override void Parse()
        {
            ParseChildFiles();
        }

        protected override void ParseChildFiles()
        {
            try
            {
                if (!ValidateRequiredFiles())
                    return;

                using (XmlReader reader = XmlReader.Create($@"Xml\{XmlBaseFolder}\{XmlParentFile}"))
                {
                    reader.MoveToContent();
                    if (reader.Name != XmlBaseFolder)
                        return;

                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            string fontGlow = reader["fontglow"];

                            if (reader.Read())
                            {
                                HomeScreenBackgrounds.Add(new Tuple<BitmapImage, Color>(SetHomeScreenBitmapImage(reader.Value), (Color)ColorConverter.ConvertFromString(fontGlow)));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ParseXmlException("Error on parsing of xml files", ex);
            }
        }

        private BitmapImage SetHomeScreenBitmapImage(string fileName)
        {
            return HeroesBitmapImage($@"Homescreens\{fileName}");
        }
    }
}

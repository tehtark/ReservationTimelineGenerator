﻿using ReservationTimelineGenerator.Models;
using ReservationTimelineGenerator.Services.Interfaces;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;

namespace ReservationTimelineGenerator.Services;

[SupportedOSPlatform("windows")]
internal class ImageService : IImageService
{
    public void GenerateImage(List<Reservation> records)
    {
        // Gantt Chart Configuration
        int chartWidth = 1920;
        int chartHeight = 1080;
        int rowHeight = 30;
        int barThickness = 28;
        int margin = 40;
        int reservationTime = 2;
        int totalTables = 15;
        StringFormat stringFormat = new StringFormat();
        stringFormat.Alignment = StringAlignment.Center;
        stringFormat.LineAlignment = StringAlignment.Near;
        Pen axisPen = new Pen(Color.Black, 2);
        Pen reservationPen = new Pen(Color.Gray, 1);
        Brush reservationBrush = Brushes.LightBlue;
        Font font = SystemFonts.DefaultFont;

        var date = DateTime.Parse(records[0].Time).Date;
        var firstTable = DateTime.Parse(records.First().Time).Hour - 1;
        var lastTable = DateTime.Parse(records.Last().Time).Hour + 3;

        int dayLength = lastTable - firstTable;
        // Calculate time unit width dynamically to fit the chart
        int timeUnitWidth = (chartWidth - 2 * margin) / dayLength;

        // Create Bitmap
        Bitmap bitmap = new(chartWidth, chartHeight);
        Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);

        // Draw Horizontal Axis
        graphics.DrawLine(axisPen, margin, margin + 10, chartWidth - margin, margin + 10);

        // Draw Vertical Axis
        graphics.DrawLine(axisPen, margin, margin, margin, chartHeight - margin);

        // Draw timeslot markings (every 15 minutes)
        for (double time = firstTable; time <= lastTable; time += 0.25) // 0.25 represents 15 minutes
        {
            int x = margin + (int)((time - firstTable) * timeUnitWidth);
            int lineHeight = time % 1 == 0 ? 10 : 5;
            graphics.DrawLine(axisPen, x, margin, x, margin + lineHeight);
            if (time % 1 == 0 || time % 1 == 0.5) {
                int hour = (int)time;
                int minute = (int)((time - hour) * 60); // Calculate minutes from fractional part
                string timeLabel = $"{hour % 12}:{minute:00} {(hour < 12 ? "AM" : "PM")}"; // Corrected format
                graphics.DrawString(timeLabel, SystemFonts.DefaultFont, Brushes.Black, x, margin - 25, stringFormat);
            }
        }

        // Draw Reservation Blocks by Table
        for (int t = 0; t < totalTables; t++) {
            int y = margin + t * rowHeight;
            graphics.DrawString((t + 1).ToString(), font, Brushes.Black, margin - 25, y + rowHeight / 2 + 3, stringFormat);
            graphics.DrawLine(reservationPen, margin, y + margin, chartWidth - margin, y + margin);
            for (int i = 0; i < records.Count; i++) {
                Reservation r = records[i];
                string[] tables = r.Table.Split('+');

                // Check if the current table is included in the reservation's tables
                if (tables.Contains((t + 1).ToString())) {
                    // Parse Time
                    DateTime startTime = DateTime.Parse(r.Time);
                    DateTime endTime = startTime.AddHours(reservationTime);

                    // Calculate start and end X positions
                    int barStartX = margin + (int)((startTime.Hour - firstTable) * timeUnitWidth + startTime.Minute / 60.0 * timeUnitWidth);
                    int barEndX = margin + (int)((endTime.Hour - firstTable) * timeUnitWidth + endTime.Minute / 60.0 * timeUnitWidth);
                    int barWidth = Math.Max(barEndX - barStartX, 1); // Ensure minimum width of 1 pixel

                    // Check if reservation extends beyond the chart
                    if (barEndX > chartWidth - margin) {
                        barWidth = chartWidth - margin - barStartX; // Limit width to fit the chart
                    }

                    // Draw reservation block with adjusted positions
                    graphics.FillRectangle(reservationBrush, barStartX, y + (rowHeight - barThickness) / 2 + 10, barWidth, barThickness);

                    int textX = (barStartX + barEndX) / 2;
                    int textY = y + (rowHeight / 2 + 2);
                    graphics.DrawString(r.Name + $" - Covers: {r.Covers}", font, Brushes.Black, textX, textY, stringFormat);
                }
                // Save Bitmap
            }
        }
        SaveImage(bitmap, $"Table Reservations Gantt Chart - {date.ToString("yyyy-MM-dd")}.png");
    }

    public void SaveImage(Bitmap bitmap, string fileName)
    {
        try {
            bitmap.Save("output/" + fileName);
        }
        catch (Exception error) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Image exported to: output/{fileName}");
        Thread.Sleep(1000);
        Console.ResetColor();
        Process.Start("explorer.exe", "/open," + AppDomain.CurrentDomain.BaseDirectory + @"output\" + fileName);
    }

    public void DrawAxis(Graphics graphics, int margin, int chartWidth, int chartHeight)
    {
        throw new NotImplementedException();
    }
}
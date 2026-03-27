using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetBoilerplate.Utils
{
    /// <summary>
    /// Billing calculation helper for consistent calculations across the application.
    /// Handles all item amount calculations with proper rounding to avoid precision errors.
    /// </summary>
    public static class BillingHelper
    {
        // ============================================================================
        // CORE ROUNDING UTILITIES
        // ============================================================================

        /// <summary>
        /// Round to specified decimal places
        /// </summary>
        /// <param name="num">Number to round</param>
        /// <param name="decimals">Number of decimal places (default: 2)</param>
        /// <returns>Rounded decimal</returns>
        public static decimal RoundTo(decimal num, int decimals = 2)
        {
            return Math.Round(num, decimals, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Round to nearest integer (for currency amounts)
        /// </summary>
        /// <param name="num">Number to round</param>
        /// <returns>Rounded integer</returns>
        public static int RoundToInt(decimal num)
        {
            return (int)Math.Round(num, MidpointRounding.AwayFromZero);
        }

        // ============================================================================
        // POLYTHENE MODELS
        // ============================================================================

        public class PolytheneRow
        {
            public decimal Count { get; set; }
            public decimal Weight { get; set; }
        }

        public class PolytheneBackend
        {
            public decimal NoOfPPs { get; set; }
            public decimal Weight { get; set; }
        }

        // ============================================================================
        // WEIGHT CALCULATIONS
        // ============================================================================

        /// <summary>
        /// Calculate total polythene weight
        /// </summary>
        /// <param name="ppRows">List of polythene objects</param>
        /// <returns>Total polythene weight</returns>
        public static decimal CalculateTotalPP(List<PolytheneRow> ppRows)
        {
            if (ppRows == null || ppRows.Count == 0)
                return 0;

            var total = ppRows.Sum(pp => pp.Count * pp.Weight);
            return RoundTo(total, 3); // Keep 3 decimals for weight precision
        }

        /// <summary>
        /// Calculate net weight (gross weight - polythene weight)
        /// </summary>
        /// <param name="grossWeight">Gross weight</param>
        /// <param name="ppRows">List of polythene objects</param>
        /// <returns>Net weight</returns>
        public static decimal CalculateNetWeight(decimal grossWeight, List<PolytheneRow> ppRows)
        {
            var totalPP = CalculateTotalPP(ppRows);
            var netWeight = Math.Max(0, grossWeight - totalPP);
            return RoundTo(netWeight, 3); // Keep 3 decimals for weight precision
        }

        // ============================================================================
        // LABOUR CALCULATIONS
        // ============================================================================

        /// <summary>
        /// Calculate labour amount based on type
        /// </summary>
        /// <param name="labourType">'P' (per piece), 'K' (per kg), 'G' (per gram)</param>
        /// <param name="labourRate">Labour rate</param>
        /// <param name="netWeight">Net weight in grams</param>
        /// <param name="numPieces">Number of pieces (for type 'P')</param>
        /// <returns>Labour amount</returns>
        public static int CalculateLabourAmount(string labourType, decimal labourRate, decimal netWeight, int numPieces = 0)
        {
            decimal labourAmount = 0;

            switch (labourType?.ToUpper())
            {
                case "P": // Per Piece
                    labourAmount = labourRate * numPieces;
                    break;

                case "K": // Per Kilogram
                    labourAmount = (labourRate / 1000m) * netWeight; // Convert weight to kg
                    break;

                case "G": // Per Gram
                    labourAmount = labourRate * netWeight;
                    break;

                default:
                    labourAmount = 0;
                    break;
            }

            return RoundToInt(labourAmount);
        }

        // ============================================================================
        // RETAIL CUSTOMER CALCULATIONS (cusType = 'R')
        // ============================================================================

        /// <summary>
        /// Calculate item amount for RETAIL customer using rate per gram
        /// Formula: rateGm × netWeight
        /// </summary>
        /// <param name="rateGm">Rate per gram</param>
        /// <param name="netWeight">Net weight in grams</param>
        /// <returns>Item amount</returns>
        public static int CalculateRetailAmountByRateGm(decimal rateGm, decimal netWeight)
        {
            if (rateGm <= 0 || netWeight <= 0)
                return 0;

            var amount = rateGm * netWeight;
            return RoundToInt(amount);
        }

        /// <summary>
        /// Calculate item amount for RETAIL customer using rate percentage on silver
        /// Formula: (ratePer% × silverRate) ÷ 1000 × netWeight
        /// </summary>
        /// <param name="ratePer">Rate percentage</param>
        /// <param name="silverRate">Silver rate per kg</param>
        /// <param name="netWeight">Net weight in grams</param>
        /// <returns>Item amount</returns>
        public static int CalculateRetailAmountByRatePer(decimal ratePer, decimal silverRate, decimal netWeight)
        {
            if (ratePer <= 0 || silverRate <= 0 || netWeight <= 0)
                return 0;

            // Step 1: Apply rate percentage on silver rate
            var silverWithRate = (ratePer / 100m) * silverRate;

            // Step 2: Convert silver rate from per kg to per gram
            var silverPerGram = silverWithRate / 1000m;

            // Step 3: Multiply by net weight
            var amount = silverPerGram * netWeight;

            return RoundToInt(amount);
        }

        /// <summary>
        /// Calculate total item amount for RETAIL customer
        /// Automatically chooses between rateGm or ratePer method
        /// </summary>
        /// <param name="grossWeight">Gross weight</param>
        /// <param name="ppRows">Polythene rows</param>
        /// <param name="rateGm">Rate per gram (optional)</param>
        /// <param name="rateKg">Rate per kilogram (optional)</param>
        /// <param name="ratePer">Rate percentage (optional)</param>
        /// <param name="silverRate">Silver rate per kg (required if using ratePer)</param>
        /// <returns>Item amount</returns>
        public static int CalculateRetailItemAmount(
            decimal grossWeight,
            List<PolytheneRow> ppRows,
            decimal? rateGm,
            decimal? rateKg,
            decimal? ratePer,
            decimal silverRate)
        {
            var netWeight = CalculateNetWeight(grossWeight, ppRows);

            // Priority 1: Use rateGm if available
            if (rateGm.HasValue && rateGm.Value > 0)
            {
                return CalculateRetailAmountByRateGm(rateGm.Value, netWeight);
            }

            // Priority 2: Use rateKg if available  // ⭐ ADD THIS BLOCK
            if (rateKg.HasValue && rateKg.Value > 0)
            {
                return CalculateRetailAmountByRateKg(rateKg.Value, netWeight);
            }

            // Priority 3: Use ratePer if available
            if (ratePer.HasValue && ratePer.Value > 0 && silverRate > 0)
            {
                return CalculateRetailAmountByRatePer(ratePer.Value, silverRate, netWeight);
            }

            return 0;
        }

        // ============================================================================
        // WHOLESALE CUSTOMER CALCULATIONS (cusType = 'W')
        // ============================================================================

        /// <summary>
        /// Calculate item amount for WHOLESALE customer using rate per gram
        /// Formula: (rateGm × netWeight) + labourAmount
        /// </summary>
        /// <param name="rateGm">Rate per gram</param>
        /// <param name="netWeight">Net weight in grams</param>
        /// <param name="labourAmount">Pre-calculated labour amount</param>
        /// <returns>Item amount</returns>
        public static int CalculateWholesaleAmountByRateGm(decimal rateGm, decimal netWeight, int labourAmount = 0)
        {
            if (rateGm <= 0 || netWeight <= 0)
                return labourAmount;

            var silverAmount = rateGm * netWeight;
            var totalAmount = silverAmount + labourAmount;

            return RoundToInt(totalAmount);
        }

        /// <summary>
        /// Calculate item amount for WHOLESALE customer using rate percentage
        /// Formula: (fine × silverRatePerGram) + labourAmount
        /// where fine = netWeight × (ratePer / 100)
        /// </summary>
        /// <param name="ratePer">Rate percentage</param>
        /// <param name="silverRate">Silver rate per kg</param>
        /// <param name="netWeight">Net weight in grams</param>
        /// <param name="labourAmount">Pre-calculated labour amount</param>
        /// <returns>Item amount</returns>
        public static int CalculateWholesaleAmountByRatePer(
            decimal ratePer,
            decimal silverRate,
            decimal netWeight,
            int labourAmount = 0)
        {
            if (ratePer <= 0 || silverRate <= 0 || netWeight <= 0)
                return labourAmount;

            // Step 1: Calculate fine (apply rate percentage on net weight)
            //var fine = netWeight * (ratePer / 100m);
            var fine = RoundTo(netWeight * (ratePer / 100m), 2);

            // Step 2: Convert silver rate from per kg to per gram
            var silverPerGram = silverRate / 1000m;

            // Step 3: Calculate silver amount and add labour
            var silverAmount = fine * silverPerGram;
            var totalAmount = silverAmount + labourAmount;

            return RoundToInt(totalAmount);
        }

        // ============================================================================
        // CALCULATION RESULT MODEL
        // ============================================================================

        public class ItemCalculationResult
        {
            public int Amount { get; set; }
            public int LabourAmount { get; set; }
            public decimal NetWeight { get; set; }
        }

        /// <summary>
        /// Calculate total item amount for WHOLESALE customer
        /// Automatically chooses between rateGm or ratePer method and includes labour
        /// </summary>
        /// <param name="grossWeight">Gross weight</param>
        /// <param name="ppRows">Polythene rows</param>
        /// <param name="rateGm">Rate per gram (optional)</param>
        /// <param name="rateKg">Rate per kilogram (optional)</param>
        /// <param name="ratePer">Rate percentage (optional)</param>
        /// <param name="silverRate">Silver rate per kg (required if using ratePer)</param>
        /// <param name="labourType">Labour type ('P', 'K', 'G')</param>
        /// <param name="labourRate">Labour rate</param>
        /// <param name="labourNumPieces">Number of pieces (for type 'P')</param>
        /// <returns>Calculation result with amount, labour amount, and net weight</returns>
        public static ItemCalculationResult CalculateWholesaleItemAmount(
            decimal grossWeight,
            List<PolytheneRow> ppRows,
            decimal? rateGm,
            decimal? rateKg,
            decimal? ratePer,
            decimal silverRate,
            string labourType,
            decimal labourRate,
            int labourNumPieces)
        {
            var netWeight = CalculateNetWeight(grossWeight, ppRows);

            // Calculate labour first
            var labourAmount = CalculateLabourAmount(labourType, labourRate, netWeight, labourNumPieces);

            int amount = 0;

            // Priority 1: Use rateGm if available
            if (rateGm.HasValue && rateGm.Value > 0)
            {
                amount = CalculateWholesaleAmountByRateGm(rateGm.Value, netWeight, labourAmount);
            }
            // Priority 2: Use rateKg if available  
            else if (rateKg.HasValue && rateKg.Value > 0)
            {
                amount = CalculateWholesaleAmountByRateKg(rateKg.Value, netWeight, labourAmount);
            }
            // Priority 3: Use ratePer if available
            else if (ratePer.HasValue && ratePer.Value > 0 && silverRate > 0)
            {
                amount = CalculateWholesaleAmountByRatePer(ratePer.Value, silverRate, netWeight, labourAmount);
            }
            // No rate specified, only labour
            else
            {
                amount = labourAmount;
            }

            return new ItemCalculationResult
            {
                Amount = amount,
                LabourAmount = labourAmount,
                NetWeight = netWeight
            };
        }

        // ============================================================================
        // UNIFIED CALCULATION FUNCTION
        // ============================================================================

        /// <summary>
        /// Calculate item amount based on customer type
        /// Main function to use throughout the application
        /// </summary>
        /// <param name="cusType">Customer type ('R' for Retail, 'W' for Wholesale)</param>
        /// <param name="grossWeight">Gross weight</param>
        /// <param name="ppRows">Polythene rows</param>
        /// <param name="rateGm">Rate per gram (optional)</param>
        /// <param name="ratePer">Rate percentage (optional)</param>
        /// <param name="silverRate">Silver rate per kg</param>
        /// <param name="labourType">Labour type ('P', 'K', 'G')</param>
        /// <param name="labourRate">Labour rate</param>
        /// <param name="labourNumPieces">Number of pieces (for type 'P')</param>
        /// <returns>Calculation result with amount, labour amount, and net weight</returns>
        public static ItemCalculationResult CalculateItemAmount(
            string cusType,
            decimal grossWeight,
            List<PolytheneRow> ppRows,
            decimal? rateGm = null,
            decimal? rateKg = null,
            decimal? ratePer = null,
            decimal silverRate = 0,
            string labourType = null,
            decimal labourRate = 0,
            int labourNumPieces = 0)
        {
            var netWeight = CalculateNetWeight(grossWeight, ppRows ?? new List<PolytheneRow>());

            if (cusType?.ToUpper() == "R")
            {
                // RETAIL CUSTOMER
                var amount = CalculateRetailItemAmount(grossWeight, ppRows, rateGm, rateKg, ratePer, silverRate);
                return new ItemCalculationResult
                {
                    Amount = amount,
                    LabourAmount = 0, // Retail doesn't use labour
                    NetWeight = netWeight
                };
            }
            else if (cusType?.ToUpper() == "W")
            {
                // WHOLESALE CUSTOMER
                return CalculateWholesaleItemAmount(
                    grossWeight,
                    ppRows,
                    rateGm,
                    rateKg,
                    ratePer,
                    silverRate,
                    labourType,
                    labourRate,
                    labourNumPieces
                );
            }

            // Unknown customer type
            return new ItemCalculationResult
            {
                Amount = 0,
                LabourAmount = 0,
                NetWeight = netWeight
            };
        }

        // ============================================================================
        // RATE PER KILOGRAM CALCULATIONS
        // ============================================================================

        /// <summary>
        /// Calculate item amount for RETAIL customer using rate per kilogram
        /// Formula: (rateKg / 1000) × netWeight
        /// </summary>
        /// <param name="rateKg">Rate per kilogram</param>
        /// <param name="netWeight">Net weight in grams</param>
        /// <returns>Item amount</returns>
        public static int CalculateRetailAmountByRateKg(decimal rateKg, decimal netWeight)
        {
            if (rateKg <= 0 || netWeight <= 0)
                return 0;

            // Convert rate per kg to rate per gram
            var ratePerGram = rateKg / 1000m;
            var amount = ratePerGram * netWeight;
            return RoundToInt(amount);
        }

        /// <summary>
        /// Calculate item amount for WHOLESALE customer using rate per kilogram
        /// Formula: ((rateKg / 1000) × netWeight) + labourAmount
        /// </summary>
        /// <param name="rateKg">Rate per kilogram</param>
        /// <param name="netWeight">Net weight in grams</param>
        /// <param name="labourAmount">Pre-calculated labour amount</param>
        /// <returns>Item amount</returns>
        public static int CalculateWholesaleAmountByRateKg(decimal rateKg, decimal netWeight, int labourAmount = 0)
        {
            if (rateKg <= 0 || netWeight <= 0)
                return labourAmount;

            // Convert rate per kg to rate per gram
            var ratePerGram = rateKg / 1000m;
            var silverAmount = ratePerGram * netWeight;
            var totalAmount = silverAmount + labourAmount;

            return RoundToInt(totalAmount);
        }

        // ============================================================================
        // POLYTHENE TRANSFORMATION HELPERS
        // ============================================================================

        /// <summary>
        /// Transform ppRows - swap count and weight if count is decimal
        /// </summary>
        /// <param name="ppRows">List of polythene rows</param>
        /// <returns>Transformed ppRows</returns>
        public static List<PolytheneRow> TransformPPRows(List<PolytheneRow> ppRows)
        {
            if (ppRows == null || ppRows.Count == 0)
                return new List<PolytheneRow>();

            return ppRows.Select(pp =>
            {
                var count = pp.Count;
                var weight = pp.Weight;

                // If count is decimal, swap with weight
                var isDecimal = count % 1 != 0;

                if (isDecimal)
                {
                    return new PolytheneRow
                    {
                        Count = weight,
                        Weight = count
                    };
                }

                return new PolytheneRow
                {
                    Count = count,
                    Weight = weight
                };
            }).ToList();
        }

        /// <summary>
        /// Convert ppRows to polythenes array format for backend storage
        /// </summary>
        /// <param name="ppRows">Transformed ppRows</param>
        /// <returns>List of polythenes for backend</returns>
        public static List<PolytheneBackend> FormatPolythenes(List<PolytheneRow> ppRows)
        {
            if (ppRows == null || ppRows.Count == 0)
                return new List<PolytheneBackend>();

            return ppRows
                .Where(pp => pp.Count > 0 && pp.Weight > 0)
                .Select(pp => new PolytheneBackend
                {
                    NoOfPPs = pp.Count,
                    Weight = pp.Weight
                })
                .ToList();
        }
    }
}
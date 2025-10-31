using System;
using DataBaseStructure.AriaBase;
using DataBaseStructure;
using DataWritingTools;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindingHAPatients
{
    public class OutPatient
    {
        public string PatientID { get; set; }
        public string DateTreated { get; set; }
        public string CourseName { get; set; }
        public string PlanName { get; set; }
        public double DosePerFraction { get; set; }
        public int NumberOfFractions { get; set; }
        public double TotalDose { get; set; }
        public double HippoMinDose { get; set; }
        public double Brain80Dose { get; set; }
    }
    public class FindHAPatientsClass
    {
        static List<OutPatient> FindModulationPatients(List<PatientClass> patients)
        {
            List<OutPatient> outPatients = new List<OutPatient>();
            foreach (PatientClass patient in patients)
            {
                foreach (CourseClass course in patient.Courses)
                {
                    foreach (TreatmentPlanClass planClass in course.TreatmentPlans)
                    {
                        if (planClass.Review.ApprovalStatus != "TreatmentApproved")
                        {
                            continue;
                        }
                        if (planClass.PlanType == "ExternalBeam")
                        {
                            foreach (BeamSetClass beamSet in planClass.BeamSets)
                            {
                                FractionDoseClass fractionDoseClass = beamSet.FractionDose;
                                if (fractionDoseClass == null)
                                {
                                    continue;
                                }
                                PrescriptionClass prescription = beamSet.Prescription;
                                double dose = 0;
                                int numberOfFractions = 0;
                                if (prescription == null)
                                {
                                    continue;
                                }
                                foreach (var target in prescription.PrescriptionTargets)
                                {
                                    if (target.DosePerFraction > dose)
                                    {
                                        dose = target.DosePerFraction;
                                        numberOfFractions = target.NumberOfFractions;
                                    }
                                }
                                if (dose != 300 || numberOfFractions != 10)
                                {
                                    continue;
                                }
                                bool hasHippo = false;
                                double hippoMindose = 0.0;
                                double brain80Dose = 0.0;
                                foreach (RegionOfInterestDose roiDose in fractionDoseClass.DoseROIs)
                                {
                                    if(roiDose.Name.ToLower().Contains("hippo"))
                                    {
                                        hasHippo = true;
                                        if (roiDose.AbsoluteDose.Min() > hippoMindose)
                                        {
                                            hippoMindose = roiDose.AbsoluteDose.Min();
                                        }
                                    }
                                    if (roiDose.Name.ToLower() == "brain" || roiDose.Name.ToLower() == "brain_ctv")
                                    {
                                        brain80Dose = roiDose.AbsoluteDose[80];
                                    }
                                }
                                if (!hasHippo)
                                {
                                    continue;
                                }
                                OutPatient outPatient = new OutPatient()
                                {
                                    PatientID = patient.MRN,
                                    DateTreated = $"{planClass.Review.ReviewTime.Month:D2}" +
    $"/{planClass.Review.ReviewTime.Day:D2}" +
    $"/{planClass.Review.ReviewTime.Year}",
                                    CourseName = course.Name,
                                    PlanName = planClass.PlanName,
                                    DosePerFraction = dose,
                                    NumberOfFractions = numberOfFractions,
                                    TotalDose = dose * numberOfFractions,
                                    Brain80Dose = brain80Dose,
                                    HippoMinDose = hippoMindose
                                };

                                outPatients.Add(outPatient);
                            }
                        }
                    }
                }
            }
            return outPatients;
        }
        public static void MainRun()
        {
            string dataDirectory = @"\\ad.ucsd.edu\ahs\CANC\RADONC\BMAnderson\DataBases";
            List<string> jsonFiles = new List<string>();
            jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2025", jsonFiles, "*.json", SearchOption.AllDirectories);
            jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2024", jsonFiles, "*.json", SearchOption.AllDirectories);
            jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2023", jsonFiles, "*.json", SearchOption.AllDirectories);
            jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2022", jsonFiles, "*.json", SearchOption.AllDirectories);
            jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2021", jsonFiles, "*.json", SearchOption.AllDirectories);
            jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2020", jsonFiles, "*.json", SearchOption.AllDirectories);
            jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2019", jsonFiles, "*.json", SearchOption.AllDirectories);
            // Started doing these in 2020!
            List<PatientClass> allPatients = new List<PatientClass>();
            allPatients = AriaDataBaseJsonReader.ReadPatientFiles(jsonFiles);
            var modulationPatients = FindModulationPatients(allPatients);
            string outputCsvPath = Path.Combine(dataDirectory, "HAPatients.csv");
            CsvTools.WriteToCsv<OutPatient>(modulationPatients, outputCsvPath);
        }
    }
}

#!network newciphermedical

build() {
  name "New Cipher Medical"
  isp district_downtown
  
  domain newciphermedical.org
  domain www.newciphermedical.org
  
  device web "Web Server"
  device intranet "Hospital Intranet"
  device patient_records "Patient Records"
  device ed_workstation "Workstation: E.D. Triage Desk"
  
  service enable ssh ed_workstation 22
  
  forward 22 ed_workstation 22
}
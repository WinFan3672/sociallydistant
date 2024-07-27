#!network newciphertoday

build() {
  name "New Cipher Today"
  isp district_wayland_heights
  
  domain newciphertoday.com
  domain www.newciphertoday.com
  
  device web "Web Server"
  device intranet "Local Intranet"
  device web_db "News Articles Database"
}
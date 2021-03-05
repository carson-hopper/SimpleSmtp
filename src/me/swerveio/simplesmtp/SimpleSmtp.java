package me.swerveio.simplesmtp;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.List;
import java.util.Properties;

import javax.activation.DataSource;
import javax.mail.Session;
import javax.mail.Message.RecipientType;
import javax.mail.internet.InternetAddress;
import javax.mail.internet.MimeMessage;

import me.swerveio.simplesmtp.utilities.MimeMessageParser;

public class SimpleSmtp {
	
	private static void write(OutputStreamWriter writer, String content) throws IOException {
		content += "\r\n";
		writer.write(content, 0, content.length());
		writer.flush();
	}
	
	public static void main(String[] args) throws Exception {
		System.setProperty("mail.mime.base64.ignoreerrors", "true");
		
		String hostname = InetAddress.getLocalHost().getHostName();
		
		@SuppressWarnings("resource")
		ServerSocket server = new ServerSocket(25);
		Thread thread = new Thread(new Runnable() { 
			public void run() { 
				try {
					System.out.println("Started server");
					while (true) {
						Socket socket = server.accept();
						
						System.out.println("Connected | " + socket.getInetAddress().getHostName());
						
						OutputStreamWriter writer = new OutputStreamWriter(socket.getOutputStream(), "ISO-8859-1");
						
						write(writer, "220 " + hostname + " service ready");
						
						String message = "";
						
						while (true) {
							InputStreamReader reader = new InputStreamReader(socket.getInputStream());
							
							if (socket.getInputStream().available() > 0) {
								char[] buffer = new char[socket.getInputStream().available()];
								reader.read(buffer, 0, socket.getInputStream().available());
								
								String data = String.valueOf(buffer);
								message += data;
								System.out.println(data);
								
								if (data.contains("HELO") || data.contains("EHLO")) write(writer, "250 " + hostname + ", I am glad to meet you");
								if (data.contains("MAIL FROM")) write(writer, "250 OK");
								if (data.contains("RCPT TO")) write(writer, "250 OK");
								if (data.contains("RSET")) write(writer, "250 OK");
								if (data.contains("DATA")) write(writer, "354 Send Message Content; end with <CR><LF>.<CR><LF>");
								
								if (data.contains("\r\n.\r\n")) write(writer, "250 OK, message accepted for delivery");
								if (data.contains("QUIT")) {
									try {
										write(writer, "221 Bye");
									} catch (Exception e) {}
									
									writer.close();
									reader.close();
									socket.close();
									
									break;
								}
							}
						}
						
						try {
							message = message.substring(0, message.indexOf("\r\n.\r\n"));
							
							Properties props = System.getProperties(); 
							Session session = Session.getInstance(props, null);
							MimeMessage mail = new MimeMessage(session, new ByteArrayInputStream(message.getBytes()));
							MimeMessageParser mailParser = new MimeMessageParser(mail).parse();
							
							String from = InternetAddress.toString(mail.getFrom());
							String to = InternetAddress.toString(mail.getRecipients(RecipientType.TO));
							
							List<DataSource> attachments = mailParser.getAttachmentList();
							String content = mailParser.getPlainContent();
							
							System.out.println("To: " + to);
							System.out.println("From: " + from);
							System.out.println("Attachments: " + attachments.size());
							System.out.println("Content: " + content);
						} catch (Exception e) { }
					}
				} catch (IOException e) {}
			}
		});
		thread.start();
	}

}
